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

namespace batch;

public static class SendMail2
{
	[FunctionName("SendMail2")]
	public static async Task<IActionResult> RunAsync(
		[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
		HttpRequest req,
		ILogger log)
	{
		var notification = default(Notification);

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

		try
		{
			_ = await SendNotificationAsync(notification, log);
			return new OkResult();
		}
		catch (Exception)
		{
			return new StatusCodeResult(StatusCodes.Status502BadGateway);
		}
	}

	private static async Task<Notification?> DecodeAsync(HttpRequest req)
		=> await JsonSerializer.DeserializeAsync<Notification>(req.Body);

	private static bool IsValid(Notification notification)
		=> !string.IsNullOrWhiteSpace(notification.body)
			&& !string.IsNullOrWhiteSpace(notification.raw)
			&& !string.IsNullOrWhiteSpace(notification.subject)
			&& notification.to.Where(user => !string.IsNullOrWhiteSpace(user)).Count() > 0;

	private static async Task<bool> SendNotificationAsync(Notification notification, ILogger log)
	{
		var users = string.Join(" ", notification.to);

		log.LogInformation("Sending notification to <{Users}>", users);

		var success = true;
		var error = default(string?);

		try
		{
			(success, error) = await SendMailAsync(notification);
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

		var response = "2."; //await client.SendAsync(message);
		await client.DisconnectAsync(true);

		return (success: response.StartsWith("2."), error: response);
	}
}
