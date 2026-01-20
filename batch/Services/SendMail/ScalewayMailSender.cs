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

namespace batch.Services.SendMail;

internal class ScalewayMailSender(IHttpClientFactory httpClientFactory) : SingleRecipientMailSender
{
	private static readonly string ReplyTo = "eXZhbkBhbGVydGVnZWxlZS5mcg==";

	private readonly HttpClient httpClient = httpClientFactory.CreateClient("default");

	public override async Task<(bool success, string? error)> SendMailAsync(string recipient, Notification notification)
	{
		var message = new ScalewayApiEmailRequest
		{
			from = new()
			{
				name = notification.from?.displayName,
				email = notification.from?.address
			},
			to = [new ScalewayApiIdentity { name = recipient, email = recipient }],
			subject = notification.subject,
			project_id = AppSettings.ScalewayProjectId,
			text = notification.raw,
			html = notification.body,
			attachments = [],
			additional_headers = [.. BuildScalewayHeaders(recipient, notification.rowKey, notification.lang ?? "fr")]
		};

		var requestContent = new StringContent(JsonSerializer.Serialize(message), Encoding.UTF8, "application/json");
		requestContent.Headers.Add("X-Auth-Token", AppSettings.ScalewayApiKey);

		async ValueTask<HttpResponseMessage> request(CancellationToken cancellationToken)
		{
			HttpResponseMessage httpResponse;
			httpResponse = await httpClient.PostAsync(AppSettings.ScalewayApiUrl, requestContent, cancellationToken);
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

	private static IEnumerable<ScalewayApiHeader> BuildScalewayHeaders(string recipient, string? subscriptionId, string language)
	{
		var replyTo = Encoding.UTF8.GetString(Convert.FromBase64String(ReplyTo));

		yield return new()
		{
			key = "Reply-To",
			value = replyTo
		};

		var listUnsubscribeHeaders = Unsubscribe.GetListUnsubscribeHeaders(replyTo, recipient, subscriptionId,language);
		foreach (var header in listUnsubscribeHeaders)
		{
			yield return new()
			{
				key = header.Key,
				value = header.Value
			};
		}
	}
}
