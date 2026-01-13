using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;
using System.Web;
using Microsoft.Extensions.Azure;
using Azure.Data.Tables;
using support.Model;

namespace support;

public class CheckSubscription
{
	private readonly TableClient checkSubscriptionEntities;

	public CheckSubscription(IAzureClientFactory<TableClient> azureClientFactory)
	{
		checkSubscriptionEntities = azureClientFactory.CreateClient("checksubscriptionTableClient");
#if DEBUG
		_ = checkSubscriptionEntities.CreateIfNotExistsAsync();
#endif
	}

	[Function("CheckSubscription")]
	public async Task<IActionResult> RunAsync(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subscription/check")]
		HttpRequest request)
	{
		var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
		var requestParams = HttpUtility.ParseQueryString(requestBody);
		if (!IsValid(requestParams))
		{
			return new BadRequestResult();
		}

		var checkSubscriptionEntity = ParseCheckSubscriptionEntity(requestParams);

		await checkSubscriptionEntities.AddEntityAsync(checkSubscriptionEntity);

		var siteUrl = requestParams["lang"] == "en" ? AppSettings.SiteEnUrl : AppSettings.SiteFrUrl;
		var checkSubscriptionCompleteUrl = siteUrl + "check-subscription-complete.html";
		request.HttpContext.Response.Headers.Append("Location", checkSubscriptionCompleteUrl);
		return new StatusCodeResult(StatusCodes.Status303SeeOther);
	}

	private static bool IsValid(NameValueCollection requestParams)
		=> requestParams["email"]?.Contains('@') is true
			&& requestParams["lang"]?.Length is > 0;

	private static CheckSubscriptionEntity ParseCheckSubscriptionEntity(NameValueCollection requestParams)
		=> new()
		{
			PartitionKey = nameof(CheckSubscriptionEntity),
			RowKey = Guid.NewGuid().ToString(),
			email = requestParams["email"],
		};
}
