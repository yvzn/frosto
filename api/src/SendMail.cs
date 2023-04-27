using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using api.Data;
using Azure.Storage.Queues.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Cryptography;

namespace api;

internal class MailQueueProcessor
{
	private Func<Notification, Task<(bool success, string? error)>> sendMailCallback;

	public MailQueueProcessor(Func<Notification, Task<(bool success, string? error)>> sendMailCallback)
	{
		this.sendMailCallback = sendMailCallback;
	}

	public async Task ProcessQueueMessageAsync(QueueMessage queueMessage, ILogger log)
	{
		var notification = Decode(queueMessage);
		if (notification is not null && IsValid(notification))
		{
			_ = await SendNotificationAsync(notification, log);
		}
		else
		{
			log.LogWarning("Skip sending notification {NotificationSubject} to <{Users}> : invalid", notification?.subject, string.Join(" ", notification?.to ?? Array.Empty<string>()));
		}
	}

	private static Notification? Decode(QueueMessage queueMessage)
	{
		var json = DecodeBase64(queueMessage.MessageText);
		return JsonSerializer.Deserialize<Notification>(json);
	}

	private static string DecodeBase64(string base64)
		=> Encoding.UTF8.GetString(Convert.FromBase64String(base64));

	private static bool IsValid(Notification notification)
		=> !string.IsNullOrWhiteSpace(notification.body)
			&& !string.IsNullOrWhiteSpace(notification.raw)
			&& !string.IsNullOrWhiteSpace(notification.subject)
			&& notification.to.Where(user => !string.IsNullOrWhiteSpace(user)).Count() > 0;

	private async Task<bool> SendNotificationAsync(Notification notification, ILogger log)
	{
		var users = string.Join(" ", notification.to);

		log.LogInformation("Sending notification to <{Users}>", users);

		var success = true;
		var error = default(string?);

		try
		{
			(success, error) = await sendMailCallback.Invoke(notification);
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
}

public static class SendMailDefault
{
	private static HttpClient httpClient = new();

	private static MailQueueProcessor processor = new MailQueueProcessor(SendMailDefault.SendMailAsync);

	[FunctionName("SendMailDefault")]
	public static async Task RunAsync(
		[QueueTrigger("email-default", Connection = "ALERTS_CONNECTION_STRING")]
		QueueMessage queueMessage,
		ILogger log)
	{
		await processor.ProcessQueueMessageAsync(queueMessage, log);
	}

	private static async Task<(bool success, string? error)> SendMailAsync(Notification notification)
	{
		var message = new
		{
			subject = notification.subject,
			body = notification.body,
			to = notification.to,
		};

		var requestContent = new StringContent(JsonSerializer.Serialize(message), Encoding.UTF8, "application/json");

		var response = await httpClient.PostAsync(AppSettings.SendMailApiUrl, requestContent);

		if (!response.IsSuccessStatusCode)
		{
			var responseContent = await response.Content.ReadAsStringAsync();
			return (false, string.Format("{0} {1}", response.StatusCode, responseContent));
		}

		return (true, default);
	}
}

public static class SendTipiMail
{
	private static HttpClient httpClient = new();

	private static MailQueueProcessor processor = new MailQueueProcessor(SendTipiMail.SendMailAsync);

	[FunctionName("SendTipiMail")]
	public static async Task RunAsync(
		[QueueTrigger("email-tipimail", Connection = "ALERTS_CONNECTION_STRING")]
		QueueMessage queueMessage,
		ILogger log)
	{
		await processor.ProcessQueueMessageAsync(queueMessage, log);
	}

	private static async Task<(bool success, string? error)> SendMailAsync(Notification notification)
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

		var response = await httpClient.PostAsync(AppSettings.TipiMailApiUrl, requestContent);

		if (!response.IsSuccessStatusCode)
		{
			var responseContent = await response.Content.ReadAsStringAsync();
			return (false, string.Format("{0} {1}", response.StatusCode, responseContent));
		}

		return (true, default);
	}
}

public static class SendSmtpMail
{
	private static HttpClient httpClient = new();

	private static MailQueueProcessor processor = new MailQueueProcessor(SendSmtpMail.SendMailAsync);

	[FunctionName("SendSmtpMail")]
	public static async Task RunAsync(
		[QueueTrigger("email-smtp", Connection = "ALERTS_CONNECTION_STRING")]
		QueueMessage queueMessage,
		ILogger log)
	{
		await processor.ProcessQueueMessageAsync(queueMessage, log);
	}

	private static async Task<(bool success, string? error)> SendMailAsync(Notification notification)
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
		using var client = new SmtpClient();

		await client.ConnectAsync(AppSettings.SmtpUrl, port: 465, SecureSocketOptions.SslOnConnect);
		await client.AuthenticateAsync(AppSettings.SmtpLogin, AppSettings.SmtpPassword);

		var response = await client.SendAsync(message);
		await client.DisconnectAsync(true);

		return (success: response.StartsWith("2."), error: response);
	}
}
