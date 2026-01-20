using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using batch.Models;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using batch.Services.SendMail;
using Microsoft.Extensions.DependencyInjection;

namespace batch;

public class SendNotification2(
	[FromKeyedServices("tipimail")] IMailSender tipiMailSender,
	[FromKeyedServices("smtp")] IMailSender smtpMailSender,
	[FromKeyedServices("api")] IMailSender apiMailSender,
	[FromKeyedServices("scaleway")] IMailSender scalewayMailSender,
	[FromKeyedServices("default")] IMailSender defaultMailSender,
	ILogger<SendNotification2> logger)
{
	internal static ISet<string> channels = new HashSet<string>() { "default", "api", "tipimail", "smtp", "scaleway" };

	private readonly Dictionary<string, IMailSender> mailSenders = new()
	{
		{ "tipimail", tipiMailSender },
		{ "smtp", smtpMailSender },
		{ "api", apiMailSender },
		{ "scaleway", scalewayMailSender },
		{ "default", defaultMailSender }
	};

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
			&& notification.from is not null
			&& !string.IsNullOrWhiteSpace(notification.from.address)
			&& !string.IsNullOrWhiteSpace(notification.from.displayName)
			&& notification.to.Any(user => !string.IsNullOrWhiteSpace(user));

	private async Task<bool> SendNotificationAsync(Notification notification, string channel)
	{
		var users = string.Join(" ", notification.to);

		logger.LogInformation("Sending notification to <{Users}> on {ChannelName} channel", users, channel);

		var success = true;
		var error = default(string);

		try
		{
			var mailSender = BuildMailSender(channel);
			(success, error) = await mailSender.SendMailAsync(notification);
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

	private IMailSender BuildMailSender(string channel)
		=> mailSenders.TryGetValue(channel, out var mailSender) ? mailSender : mailSenders["default"];
}


