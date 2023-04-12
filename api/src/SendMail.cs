using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using api.Data;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace api;

internal class MailQueueProcessor
{
	private QueueClient queueClient;
	private Func<Notification, Task<HttpResponseMessage>> sendMail;

	public MailQueueProcessor(string queueName, Func<Notification, Task<HttpResponseMessage>> sendMail)
	{
		this.queueClient = new QueueClient(AppSettings.AlertsConnectionString, queueName, new() { MessageEncoding = QueueMessageEncoding.Base64 });
		this.sendMail = sendMail;
	}

	public async Task ProcessQueueMessageAsync(ILogger log)
	{
		var queueMessage = await DequeueMessageAsync();
		if (queueMessage is null)
		{
			return;
		}

		var success = true;

		var notification = Decode(queueMessage);
		if (notification is not null && IsValid(notification))
		{
			success = await SendNotificationAsync(notification, log);
		}

		if (success)
		{
			await OnMessageProcessedAsync(queueMessage);
		}
	}

	private async Task<QueueMessage?> DequeueMessageAsync()
	{
		if (await queueClient.ExistsAsync())
		{
			// will return null if no message
			return await queueClient.ReceiveMessageAsync();
		}
		return default;
	}

	private async Task OnMessageProcessedAsync(QueueMessage queueMessage)
	{
		await queueClient.DeleteMessageAsync(queueMessage.MessageId, queueMessage.PopReceipt);
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

		try
		{
			var response = await sendMail.Invoke(notification);

			if (!response.IsSuccessStatusCode)
			{
				log.LogError("Failed to send notification to {Users}: {StatusCode} {StatusMessage}", users, response.StatusCode, await response.Content.ReadAsStringAsync());
			}

			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			log.LogError(ex, "Failed to send notification to {Users}", string.Join(" ", users));
			return false;
		}
	}
}

public static class SendMailDefault
{
	private static HttpClient httpClient = new();

	private static MailQueueProcessor processor = new MailQueueProcessor("email-default", SendMailDefault.SendMail);

	[FunctionName("SendMailDefault")]
	public static async Task RunAsync(
		[TimerTrigger("0 */2 6-8 * * *"
#if DEBUG
			, RunOnStartup=true
#endif
		)]
		TimerInfo timerInfo,
		ILogger log)
	{
#if DEBUG
		await Task.Delay(10_000);
#endif

		await processor.ProcessQueueMessageAsync(log);
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

	private static MailQueueProcessor processor = new MailQueueProcessor("email-tipimail", SendTipiMail.SendMail);

	[FunctionName("SendTipiMail")]
	public static async Task RunAsync(
		[TimerTrigger("0 */8 6 * * *"
#if DEBUG
			, RunOnStartup=true
#endif
		)]
		TimerInfo timerInfo,
		ILogger log)
	{
#if DEBUG
		await Task.Delay(10_000);
#endif

		await processor.ProcessQueueMessageAsync(log);
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
