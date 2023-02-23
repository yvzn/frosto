using System;
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

public static class SendMail
{
	private static HttpClient httpClient = new();

	private static string sendMailApiUrl = Environment.GetEnvironmentVariable("SEND_MAIL_API_URL") ?? throw new Exception("SEND_MAIL_API_URL variable not set");

	private static string alertsConnectionString = Environment.GetEnvironmentVariable("ALERTS_CONNECTION_STRING") ?? throw new Exception("ALERTS_CONNECTION_STRING variable not set");

	private static QueueClient queueClient = new QueueClient(alertsConnectionString, "emailoutbox", new() { MessageEncoding = QueueMessageEncoding.Base64 });

	[FunctionName("SendMail")]
	public static async Task RunAsync(
		[TimerTrigger("0 */3 6-7 * * *"
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

		var queueMessage = await DequeueMessageAsync();
		if (queueMessage is null)
		{
			return;
		}

		var success = true;

		var notification = Decode(queueMessage);
		if (notification is not null && IsValid(notification))
		{
			success = await SendMailAsync(notification, log);
		}

		if (success)
		{
			await OnMessageProcessedAsync(queueMessage);
		}
	}

	private static async Task<QueueMessage?> DequeueMessageAsync()
	{
		if (await queueClient.ExistsAsync())
		{
			// will return null if no message
			return await queueClient.ReceiveMessageAsync();
		}
		return default;
	}

	private static async Task OnMessageProcessedAsync(QueueMessage queueMessage)
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
			&& notification.to.Count > 0;

	internal static async Task<bool> SendMailAsync(Notification notification, ILogger log)
	{
		var users = notification.to;
		log.LogInformation("Sending notification to {Users}", users);

		try
		{
			var requestContent = new StringContent(JsonSerializer.Serialize(notification), Encoding.UTF8, "application/json");

			var response = await httpClient.PostAsync(sendMailApiUrl, requestContent);

			if (!response.IsSuccessStatusCode)
			{
				log.LogError("Failed to send notification to {Users}: {StatusCode} {StatusMessage}", users, response.StatusCode, await response.Content.ReadAsStringAsync());
			}

			return response.IsSuccessStatusCode;
		}
		catch (Exception ex)
		{
			log.LogError(ex, "Failed to send notification to {Users}", users);
			return false;
		}
	}
}
