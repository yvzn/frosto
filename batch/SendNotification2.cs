using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using batch.Models;
using System.Text.Json;
using System.Linq;
using MimeKit.Cryptography;
using MimeKit;
using MailKit.Net.Smtp;
using batch.Services;
using MailKit.Security;
using System.Net.Http;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Azure.Functions.Worker;

namespace batch;

public class SendNotification2(IHttpClientFactory httpClientFactory, ILogger<SendNotification2> logger)
{
	internal static ISet<string> channels = new HashSet<string>() { "default", "api", "tipimail", "smtp" };

	private readonly HttpClient httpClient = httpClientFactory.CreateClient();

	[Function("SendNotification2")]
	public async Task<IActionResult> RunAsync(
		[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
		HttpRequest req)
	{
		var notification = default(Notification);
		try
		{
			notification = await DecodeAsync(req);
			if (notification is null || !IsValid(notification))
			{
				logger.LogWarning("Skip sending notification {NotificationSubject} to <{Users}> : invalid", notification?.subject, string.Join(" ", notification?.to ?? Array.Empty<string>()));
				return new BadRequestResult();
			}
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Skip sending notification {NotificationSubject} to <{Users}> : invalid", notification?.subject, string.Join(" ", notification?.to ?? Array.Empty<string>()));
			return new BadRequestResult();
		}

		var channel = Decode(req);
		if (channel is null)
		{
			logger.LogWarning("Skip sending notification {NotificationSubject} to <{Users}> on {ChannelName} channel : invalid channel", notification?.subject, string.Join(" ", notification?.to ?? Array.Empty<string>()), channel);
			return new BadRequestResult();
		}

		try
		{
			_ = SendNotificationAsync(notification, channel);
			return new OkResult();
		}
		catch (Exception)
		{
			return new StatusCodeResult(StatusCodes.Status502BadGateway);
		}
	}

	private static string? Decode(HttpRequest req)
	{
		var queryParam = req.Query["c"].ToString();
		if (channels.Contains(queryParam)) return queryParam;
		return default;
	}

	private static async Task<Notification?> DecodeAsync(HttpRequest req)
		=> await JsonSerializer.DeserializeAsync<Notification>(req.Body);

	private static bool IsValid(Notification notification)
		=> !string.IsNullOrWhiteSpace(notification.body)
			&& !string.IsNullOrWhiteSpace(notification.raw)
			&& !string.IsNullOrWhiteSpace(notification.subject)
			&& notification.to.Where(user => !string.IsNullOrWhiteSpace(user)).Any();

	private async Task<bool> SendNotificationAsync(Notification notification, string channel)
	{
		var users = string.Join(" ", notification.to);

		logger.LogInformation("Sending notification to <{Users}> on {ChannelName} channel", users, channel);

		var success = true;
		var error = default(string);

		try
		{
			var sendMailFunction = SelectSendMailCallback(channel);
			(success, error) = await sendMailFunction.Invoke(notification);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to send notification to <{Users}>: {StatusMessage}", users, error);
			throw;
		}

		if (!success)
		{
			throw new Exception(string.Format("Failed to send notification to <{0}>: {1}", users, error));
		}

		return success;
	}

	private Func<Notification, Task<(bool success, string? error)>> SelectSendMailCallback(string channel)
		=> channel switch
		{
			"tipimail" => SendTipiMailAsync,
			"smtp" => SendMailSmtpAsync,
			"api" => SendMailApiAsync,
			_ => SendMailApiAsync
		};

	private async Task<(bool success, string? error)> SendMailApiAsync(Notification notification)
	{
		var message = new
		{
			notification.subject,
			notification.body,
			notification.to,
		};

		var requestContent = new StringContent(JsonSerializer.Serialize(message), Encoding.UTF8, "application/json");

		async ValueTask<HttpResponseMessage> request(CancellationToken cancellationToken)
		{
			HttpResponseMessage httpResponse;
			httpResponse = await httpClient.PostAsync(AppSettings.SendMailApiUrl, requestContent, cancellationToken);
			return httpResponse;
		}

		var response = await RetryStrategy.For.MailApi.ExecuteAsync(request);

		if (!response.IsSuccessStatusCode)
		{
			var responseContent = await response.Content.ReadAsStringAsync();
			return (false, string.Format("{0} {1}", response.StatusCode, responseContent));
		}

		return (true, default);
	}

	private async Task<(bool success, string? error)> SendTipiMailAsync(Notification notification)
	{
		var message = new
		{
			to = notification.to.Select(user => new { address = user }).ToArray(),
			msg = new
			{
				from = new
				{
					address = "yvan@alertegelee.fr",
					personalName = "Yvan de Alertegelee.fr"
				},
				notification.subject,
				text = notification.raw,
				html = notification.body,
			},
			apiKey = AppSettings.TipiMailApiKey
		};

		var requestContent = new StringContent(JsonSerializer.Serialize(message), Encoding.UTF8, "application/json");
		requestContent.Headers.Add("X-Tipimail-ApiUser", AppSettings.TipiMailApiUser);
		requestContent.Headers.Add("X-Tipimail-ApiKey", AppSettings.TipiMailApiKey);

		async ValueTask<HttpResponseMessage> request(CancellationToken cancellationToken)
		{
			HttpResponseMessage httpResponse;
			httpResponse = await httpClient.PostAsync(AppSettings.TipiMailApiUrl, requestContent, cancellationToken);
			return httpResponse;
		}

		var response = await RetryStrategy.For.ExternalHttp.ExecuteAsync(request);

		if (!response.IsSuccessStatusCode)
		{
			var responseContent = await response.Content.ReadAsStringAsync();
			return (false, string.Format("{0} {1}", response.StatusCode, responseContent));
		}

		return (true, default);
	}

	private static async Task<(bool success, string? error)> SendMailSmtpAsync(Notification notification)
	{
		var privateKey = File.OpenRead($"{FunctionAppDirectory}{Path.DirectorySeparatorChar}dkim_private.pem");

		var signer = new DkimSigner(
			privateKey,
			domain: "alertegelee.fr",
			selector: "frosto")
		{
			HeaderCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Simple,
			BodyCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Simple,
			AgentOrUserIdentifier = "@alertegelee.fr",
			QueryMethod = "dns/txt",
		};

		// Composing the whole email
		var message = new MimeMessage();
		message.From.Add(new MailboxAddress("Yvan de Alertegelee.fr", "yvan@alertegelee.fr"));
		message.To.AddRange(notification.to.Select(user => new MailboxAddress(user, user)));
		message.Subject = notification.subject;

		var headers = new HeaderId[] { HeaderId.From, HeaderId.Subject, HeaderId.To };

		var builder = new BodyBuilder
		{
			TextBody = notification.raw,
			HtmlBody = notification.body
		};

		message.Body = builder.ToMessageBody();
		message.Prepare(EncodingConstraint.SevenBit);

		signer.Sign(message, headers);

		// Sending the email
		async ValueTask<string?> sendmail(CancellationToken cancellationToken)
		{
			using var client = new SmtpClient();

			await client.ConnectAsync(AppSettings.SmtpUrl, port: 465, SecureSocketOptions.SslOnConnect, cancellationToken);
			await client.AuthenticateAsync(AppSettings.SmtpLogin, AppSettings.SmtpPassword, cancellationToken);

			var response = default(string);
			response = await client.SendAsync(message, cancellationToken);

			await client.DisconnectAsync(true, cancellationToken);

			return response;
		}

		var response = await RetryStrategy.For.Smtp.ExecuteAsync(sendmail);

		return (success: response is not null && response.StartsWith("2."), error: response);
	}

	private static string FunctionAppDirectory
	{
		get
		{
			// https://stackoverflow.com/questions/68082798/access-functionappdirectory-in-net-5-azure-function
			var local_root = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot");
			var azure_root = $"{Environment.GetEnvironmentVariable("HOME")}/site/wwwroot";
			return local_root ?? azure_root;
		}
	}
}
