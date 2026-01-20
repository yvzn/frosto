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
		var queryParameters = request.Query;
		if (!IsValid(queryParameters))
		{
			return new BadRequestResult();
		}

		var unsubscribeEntity = ParseUnsubscribeEntity(queryParameters);

		await unsubscribeEntities.AddEntityAsync(unsubscribeEntity);

		var siteUrl = queryParameters["lang"] == "en" ? AppSettings.SiteEnUrl : AppSettings.SiteFrUrl;
		var unsubscribeCompleteUrl = siteUrl + "unsubscribe-complete.html";
		request.HttpContext.Response.Headers.Append("Location", unsubscribeCompleteUrl);
		return new StatusCodeResult(StatusCodes.Status303SeeOther);
	}

	private static bool IsValid(IQueryCollection queryParameters)
		=> queryParameters["token"].Count == 1
		&& !string.IsNullOrWhiteSpace(queryParameters["token"])
		&& queryParameters["lang"].Count == 1
		&& queryParameters["lang"].All(v => v == "en" || v == "fr");

	private static UnsubscribeEntity ParseUnsubscribeEntity(IQueryCollection queryParameters)
		=> new()
		{
			PartitionKey = nameof(UnsubscribeEntity),
			RowKey = Guid.NewGuid().ToString(),
			token = queryParameters["token"],
			lang = queryParameters["lang"],
		};
}
