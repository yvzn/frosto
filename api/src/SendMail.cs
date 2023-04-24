using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using api.Data;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace api;

internal class MailQueueProcessor
{
	private Func<Notification, Task<HttpResponseMessage>> sendMail;

	public MailQueueProcessor(Func<Notification, Task<HttpResponseMessage>> sendMail)
	{
		this.sendMail = sendMail;
	}

	public async Task ProcessQueueMessageAsync(QueueMessage queueMessage, ILogger log)
	{
		var notification = Decode(queueMessage);
		if (notification is not null && IsValid(notification))
		{
			_ = await SendNotificationAsync(notification, log);
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
			&& !string.IsNullOrWhiteSpace(notification.subject)
			&& notification.to.Where(user => !string.IsNullOrWhiteSpace(user)).Count() > 0;

	private async Task<bool> SendNotificationAsync(Notification notification, ILogger log)
	{
		var users = notification.to;
		log.LogInformation("Sending notification to {Users}", string.Join(" ", users));

		var response = await sendMail.Invoke(notification);

		if (!response.IsSuccessStatusCode)
		{
			var responseContent = await response.Content.ReadAsStringAsync();
			log.LogError("Failed to send notification to {Users}: {StatusCode} {StatusMessage}", users, response.StatusCode, responseContent);
			throw new Exception(string.Format("Failed to send notification to {0}: {1} {2}", users, response.StatusCode, responseContent));
		}

		return response.IsSuccessStatusCode;
	}
}

public static class SendMailDefault
{
	private static HttpClient httpClient = new();

	private static MailQueueProcessor processor = new MailQueueProcessor(SendMailDefault.SendMail);

	[FunctionName("SendMailDefault")]
	public static async Task RunAsync(
		[QueueTrigger("email-default", Connection = "ALERTS_CONNECTION_STRING")]
		QueueMessage queueMessage,
		ILogger log)
	{
		await processor.ProcessQueueMessageAsync(queueMessage, log);
	}

	private static Task<HttpResponseMessage> SendMail(Notification notification)
	{
		var users = notification.to;

		var message = new
		{
			subject = notification.subject,
			body = notification.body,
			to = notification.to,
		};

		var requestContent = new StringContent(JsonSerializer.Serialize(message), Encoding.UTF8, "application/json");

		return httpClient.PostAsync(AppSettings.SendMailApiUrl, requestContent);
	}
}

public static class SendTipiMail
{
	private static HttpClient httpClient = new();

	private static MailQueueProcessor processor = new MailQueueProcessor(SendTipiMail.SendMail);

	[FunctionName("SendTipiMail")]
	public static async Task RunAsync(
		[QueueTrigger("email-tipimail", Connection = "ALERTS_CONNECTION_STRING")]
		QueueMessage queueMessage,
		ILogger log)
	{
		await processor.ProcessQueueMessageAsync(queueMessage, log);
	}

	private static Task<HttpResponseMessage> SendMail(Notification notification)
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
				html = notification.body,
			},
			apiKey = AppSettings.TipiMailApiKey
		};

		var requestContent = new StringContent(JsonSerializer.Serialize(message), Encoding.UTF8, "application/json");
		requestContent.Headers.Add("X-Tipimail-ApiUser", AppSettings.TipiMailApiUser);
		requestContent.Headers.Add("X-Tipimail-ApiKey", AppSettings.TipiMailApiKey);

		return httpClient.PostAsync(AppSettings.TipiMailApiUrl, requestContent);
	}
}
