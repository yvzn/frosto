using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using api.Data;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace api;

public static class SendMail
{
	private static HttpClient httpClient = new();

	private static string sendMailApiUrl = System.Environment.GetEnvironmentVariable("SEND_MAIL_API_URL") ?? throw new Exception("SEND_MAIL_API_URL variable not set");

	[FunctionName("SendMail")]
	public static async Task RunAsync(
		[QueueTrigger("emailoutbox", Connection = "ALERTS_CONNECTION_STRING")]
		QueueMessage queueMessage,
		ILogger log)
	{
		var sendMailRequest = Decode(queueMessage);

		if (sendMailRequest is not null && IsValid(sendMailRequest))
		{
			await SendMailAsync(sendMailRequest, log);
		}
	}

	private static SendMailRequest? Decode(QueueMessage queueMessage)
	{
		var base64 = queueMessage.Body.ToString();
		var bytes = Convert.FromBase64String(base64);
		var json = Encoding.UTF8.GetString(bytes);

		return JsonSerializer.Deserialize<SendMailRequest>(json);
	}

	private static bool IsValid(SendMailRequest sendMailRequest)
		=> !string.IsNullOrWhiteSpace(sendMailRequest.body)
			&& !string.IsNullOrWhiteSpace(sendMailRequest.subject)
			&& sendMailRequest.to.Count > 0;

	private static async Task<bool> SendMailAsync(SendMailRequest sendMailRequest, ILogger log)
	{
		if (sendMailRequest is null) throw new ArgumentNullException(nameof(sendMailRequest));

		var users = sendMailRequest.to;
		log.LogInformation("Sending notification to {Users}", sendMailRequest);

		try
		{
			var requestContent = new StringContent(JsonSerializer.Serialize(sendMailRequest), Encoding.UTF8, "application/json");

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
