using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Azure.Data.Tables;
using support.Model;
using System.Collections.Specialized;
using System.Web;

namespace support;

public class Unsubscribe
{
	private readonly TableClient unsubscribeEntities;

	public Unsubscribe(IAzureClientFactory<TableClient> azureClientFactory)
	{
		unsubscribeEntities = azureClientFactory.CreateClient("unsubscribeTableClient");
#if DEBUG
		_ = unsubscribeEntities.CreateIfNotExistsAsync();
#endif
	}

	[Function("Unsubscribe")]
	public async Task<IActionResult> RunAsync(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "unsubscribe")]
		HttpRequest request)
	{
		var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
		var requestParams = HttpUtility.ParseQueryString(requestBody);
		if (!IsValid(requestParams))
		{
			return new BadRequestResult();
		}

		var unsubscribeEntity = ParseUnsubscribeEntity(requestParams);

		await unsubscribeEntities.AddEntityAsync(unsubscribeEntity);

		var siteUrl = requestParams["lang"] == "en" ? AppSettings.SiteEnUrl : AppSettings.SiteFrUrl;
		var unsubscribeCompleteUrl = siteUrl + "unsubscribe-complete.html";
		request.HttpContext.Response.Headers.Append("Location", unsubscribeCompleteUrl);
		return new StatusCodeResult(StatusCodes.Status303SeeOther);
	}

	private static bool IsValid(NameValueCollection requestParams)
		=> requestParams["token"]?.Length is > 0
			&& requestParams["lang"]?.Length is > 0;

	private static UnsubscribeEntity ParseUnsubscribeEntity(NameValueCollection requestParams)
		=> new()
		{
			PartitionKey = nameof(UnsubscribeEntity),
			RowKey = Guid.NewGuid().ToString(),
			token = requestParams["token"],
			lang = requestParams["lang"],
		};
}
