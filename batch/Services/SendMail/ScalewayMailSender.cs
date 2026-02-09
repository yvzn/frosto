using System;
using System.Threading.Tasks;
using batch.Models;
using System.Text.Json;
using System.Net.Http;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace batch.Services.SendMail;

internal class ScalewayMailSender(
	Unsubscribe unsubscribe,
	IHttpClientFactory httpClientFactory,
	ILogger<ScalewayMailSender> logger) : SingleRecipientMailSender
{
	private static readonly string ReplyTo = "eXZhbkBhbGVydGVnZWxlZS5mcg==";

	private readonly HttpClient httpClient = httpClientFactory.CreateClient("default");

	public override async Task<(bool success, string? error)> SendMailAsync(SingleRecipientNotification notification)
	{
		var unsubscribeToken = unsubscribe.BuildUnsubscribeToken(notification);
		if (!unsubscribeToken.StartsWith("ey"))
		{
			logger.LogWarning("Invalid unsubscribe token generated for recipient {recipient}", notification.to);
		}

		var unsubscribeUrl = Unsubscribe.BuildUnsubscribeUrl(unsubscribeToken, notification);
		var unsubscribeLink = HtmlFormatter.FormatUnsubscribeLink(unsubscribeUrl);

		var message = new ScalewayApiEmailRequest
		{
			from = new()
			{
				name = notification.from?.displayName,
				email = notification.from?.address
			},
			to = [new ScalewayApiIdentity { name = notification.to, email = notification.to }],
			subject = notification.subject,
			project_id = AppSettings.ScalewayProjectId,
			text = notification.raw,
			html = notification.body?.Replace(HtmlFormatter.unsubscribeLinkPlaceholder, unsubscribeLink),
			attachments = [],
			additional_headers = [.. BuildScalewayHeaders(unsubscribeToken, notification)]
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

	private static IEnumerable<ScalewayApiHeader> BuildScalewayHeaders(string unsubscribeToken, SingleRecipientNotification notification)
	{
		var replyTo = Encoding.UTF8.GetString(Convert.FromBase64String(ReplyTo));

		yield return new()
		{
			key = "Reply-To",
			value = replyTo
		};

		var listUnsubscribeHeaders = Unsubscribe.GetListUnsubscribeHeaders(replyTo, unsubscribeToken, notification);
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
