using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using batch.Models;

namespace batch.Services.SendMail;

internal class ApiMailSender(IHttpClientFactory httpClientFactory) : IBatchMailSender
{
	private readonly HttpClient httpClient = httpClientFactory.CreateClient("default");

	public async Task<(bool success, string? error)> SendMailAsync(Notification notification)
	{
		var message = new
		{
			notification.subject,
			notification.body,
			notification.to,
		};

		var requestContent = new StringContent(JsonSerializer.Serialize(message), Encoding.UTF8, "application/json");

		async ValueTask<HttpResponseMessage> request(CancellationToken cancellationToken)
		{
			HttpResponseMessage httpResponse;
			httpResponse = await httpClient.PostAsync(AppSettings.SendMailApiUrl, requestContent, cancellationToken);
			return httpResponse;
		}

		var response = await RetryStrategy.For.MailApi.ExecuteAsync(request);

		if (!response.IsSuccessStatusCode)
		{
			var responseContent = await response.Content.ReadAsStringAsync();
			return (false, string.Format("{0} {1}", response.StatusCode, responseContent));
		}

		return (true, default);
	}
}
