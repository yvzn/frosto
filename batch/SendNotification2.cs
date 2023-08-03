using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
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

namespace batch;

public static class SendNotification2
{
	private static HttpClient httpClient = new();

	internal static ISet<string> channels = new HashSet<string>() { "default", "tipimail", "smtp" };

	[FunctionName("SendNotification2")]
	public static async Task<IActionResult> RunAsync(
		[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
		HttpRequest req,
		ILogger log)
	{
		var notification = default(Notification);
		var channel = default(string);

		try
		{
			notification = await DecodeAsync(req);
			if (notification is null || !IsValid(notification))
			{
				log.LogWarning("Skip sending notification {NotificationSubject} to <{Users}> : invalid", notification?.subject, string.Join(" ", notification?.to ?? Array.Empty<string>()));
				return new BadRequestResult();
			}
		}
		catch (Exception ex)
		{
			log.LogWarning(ex, "Skip sending notification {NotificationSubject} to <{Users}> : invalid", notification?.subject, string.Join(" ", notification?.to ?? Array.Empty<string>()));
			return new BadRequestResult();
		}

		channel = Decode(req);
		if (channel is null)
		{
			log.LogWarning("Skip sending notification {NotificationSubject} to <{Users}> on {ChannelName} channel : invalid channel", notification?.subject, string.Join(" ", notification?.to ?? Array.Empty<string>()), channel);
			return new BadRequestResult();
		}

		try
		{
			_ = await SendNotificationAsync(notification, channel, log);
			return new OkResult();
		}
		catch (Exception)
		{
			return new StatusCodeResult(StatusCodes.Status502BadGateway);
		}
	}

	private static string? Decode(HttpRequest req)
	{
		var queryParam = req.Query["c"];
		if (channels.Contains(queryParam)) return queryParam;
		return default;
	}

	private static async Task<Notification?> DecodeAsync(HttpRequest req)
		=> await JsonSerializer.DeserializeAsync<Notification>(req.Body);

	private static bool IsValid(Notification notification)
		=> !string.IsNullOrWhiteSpace(notification.body)
			&& !string.IsNullOrWhiteSpace(notification.raw)
			&& !string.IsNullOrWhiteSpace(notification.subject)
			&& notification.to.Where(user => !string.IsNullOrWhiteSpace(user)).Count() > 0;

	private static async Task<bool> SendNotificationAsync(Notification notification, string channel, ILogger log)
	{
		var users = string.Join(" ", notification.to);

		log.LogInformation("Sending notification to <{Users}> on {ChannelName} channel", users, channel);

		var success = true;
		var error = default(string);

		try
		{
			var sendMailFunction = SelectSendMailCallback(channel);
			(success, error) = await sendMailFunction.Invoke(notification);
		}
		catch (Exception ex)
		{
			log.LogError(ex, "Failed to send notification to <{Users}>: {StatusMessage}", users, error);
			throw;
		}

		if (!success)
		{
			throw new Exception(string.Format("Failed to send notification to <{0}>: {1}", users, error));
		}

		return success;
	}

	private static Func<Notification, Task<(bool success, string? error)>> SelectSendMailCallback(string channel)
		=> channel switch
		{
			"tipimail" => SendNotification2.SendTipiMailAsync,
			"smtp" => SendNotification2.SendMailSmtpAsync,
			_ => SendNotification2.SendMailDefaultAsync
		};

	private static async Task<(bool success, string? error)> SendMailDefaultAsync(Notification notification)
	{
		var message = new
		{
			subject = notification.subject,
			body = notification.body,
			to = notification.to,
		};

		var requestContent = new StringContent(JsonSerializer.Serialize(message), Encoding.UTF8, "application/json");

		var request = () => httpClient.PostAsync(AppSettings.SendMailApiUrl, requestContent);
		var response = await RetryPolicy.ForExternalHttpAsync.ExecuteAsync(request);

		if (!response.IsSuccessStatusCode)
		{
			var responseContent = await response.Content.ReadAsStringAsync();
			return (false, string.Format("{0} {1}", response.StatusCode, responseContent));
		}

		return (true, default);
	}

	private static async Task<(bool success, string? error)> SendTipiMailAsync(Notification notification)
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
				subject = notification.subject,
				text = notification.raw,
				html = notification.body,
			},
			apiKey = AppSettings.TipiMailApiKey
		};

		var requestContent = new StringContent(JsonSerializer.Serialize(message), Encoding.UTF8, "application/json");
		requestContent.Headers.Add("X-Tipimail-ApiUser", AppSettings.TipiMailApiUser);
		requestContent.Headers.Add("X-Tipimail-ApiKey", AppSettings.TipiMailApiKey);

		var request = () => httpClient.PostAsync(AppSettings.TipiMailApiUrl, requestContent);
		var response = await RetryPolicy.ForExternalHttpAsync.ExecuteAsync(request);

		if (!response.IsSuccessStatusCode)
		{
			var responseContent = await response.Content.ReadAsStringAsync();
			return (false, string.Format("{0} {1}", response.StatusCode, responseContent));
		}

		return (true, default);
	}

	private static async Task<(bool success, string? error)> SendMailSmtpAsync(Notification notification)
	{
		var Signer = new DkimSigner(
			File.OpenRead("dkim_private.pem"),
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

		var builder = new BodyBuilder();
		builder.TextBody = notification.raw;
		builder.HtmlBody = notification.body;

		message.Body = builder.ToMessageBody();
		message.Prepare(EncodingConstraint.SevenBit);

		Signer.Sign(message, headers);

		// Sending the email
		var sendmail = async () =>
		{
			using var client = new SmtpClient();

			await client.ConnectAsync(AppSettings.SmtpUrl, port: 465, SecureSocketOptions.SslOnConnect);
			await client.AuthenticateAsync(AppSettings.SmtpLogin, AppSettings.SmtpPassword);

			var response = default(string);
			response = await client.SendAsync(message);

			await client.DisconnectAsync(true);

			return response;
		};

		var response = await RetryPolicy.ForSmtpAsync.ExecuteAsync(sendmail);

		return (success: response is not null && response.StartsWith("2."), error: response);
	}
}
