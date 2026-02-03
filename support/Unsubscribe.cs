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
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "unsubscribe")]
		HttpRequest request)
	{
		var queryParameters = request.Query;

		var siteUrl = queryParameters["lang"] == "en" ? AppSettings.SiteEnUrl : AppSettings.SiteFrUrl;
		var redirectionUrl = new UriBuilder(siteUrl + "unsubscribe-complete.html");

		if (IsValid(queryParameters))
		{
			var unsubscribeEntity = ParseUnsubscribeEntity(queryParameters);
			await unsubscribeEntities.AddEntityAsync(unsubscribeEntity);
		}
		else
		{
			redirectionUrl = new UriBuilder(siteUrl + "unsubscribe.html");
			var redirectionParameters = HttpUtility.ParseQueryString(string.Empty);
			redirectionParameters.Add("email", queryParameters["email"]);
			redirectionParameters.Add("id", queryParameters["id"]);
			redirectionParameters.Add("reason", queryParameters["reason"]);
			redirectionUrl.Query = redirectionParameters.ToString() ?? string.Empty;
		}

		request.HttpContext.Response.Headers.Append("Location", redirectionUrl.ToString());
		return new StatusCodeResult(StatusCodes.Status303SeeOther);
	}

	private static bool IsValid(IQueryCollection queryParameters)
		=> (IsTokenValid(queryParameters) || IsEmailValid(queryParameters))
		&& queryParameters["lang"].Count == 1
		&& queryParameters["lang"].All(v => v == "en" || v == "fr");

	private static bool IsTokenValid(IQueryCollection queryParameters)
		=> new[] { "token", "user" }
		.Any(key => queryParameters[key].Count == 1
			&& queryParameters[key].All(v => !string.IsNullOrWhiteSpace(v))
			&& queryParameters[key].All(v => v?.StartsWith("ey") is true));

	private static bool IsEmailValid(IQueryCollection queryParameters)
		=> queryParameters["email"].Count == 1
		&& !string.IsNullOrWhiteSpace(queryParameters["email"])
		&& queryParameters["id"].Count == 1
		&& Guid.TryParse(queryParameters["id"], out _);

	private static UnsubscribeEntity ParseUnsubscribeEntity(IQueryCollection queryParameters)
		=> new()
		{
			PartitionKey = nameof(UnsubscribeEntity),
			RowKey = Guid.NewGuid().ToString(),
			token = queryParameters["token"],
			user = queryParameters["user"],
			email = queryParameters["email"],
			id = Guid.TryParse(queryParameters["id"], out var guid) ? guid : default,
			reason = queryParameters["reason"],
			lang = queryParameters["lang"],
		};
}
