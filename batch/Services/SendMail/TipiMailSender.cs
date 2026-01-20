using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using batch.Models;

namespace batch.Services.SendMail;

internal class TipiMailSender(IHttpClientFactory httpClientFactory) : IBatchMailSender
{
	private readonly HttpClient httpClient = httpClientFactory.CreateClient("default");

	public async Task<(bool success, string? error)> SendMailAsync(Notification notification)
	{
		var message = new
		{
			to = notification.to.Select(user => new { address = user }).ToArray(),
			msg = new
			{
				from = new
				{
					address = notification.from?.address,
					personalName = notification.from?.displayName
				},
				notification.subject,
				text = notification.raw,
				html = notification.body,
			},
			apiKey = AppSettings.TipiMailApiKey
		};

		var requestContent = new StringContent(JsonSerializer.Serialize(message), Encoding.UTF8, "application/json");
		requestContent.Headers.Add("X-Tipimail-ApiUser", AppSettings.TipiMailApiUser);
		requestContent.Headers.Add("X-Tipimail-ApiKey", AppSettings.TipiMailApiKey);

		async ValueTask<HttpResponseMessage> request(CancellationToken cancellationToken)
		{
			HttpResponseMessage httpResponse;
			httpResponse = await httpClient.PostAsync(AppSettings.TipiMailApiUrl, requestContent, cancellationToken);
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
}
