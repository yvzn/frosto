using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Azure.Data.Tables;
using support.Model;
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

	[Function("UnsubscribePost")]
	public async Task<IActionResult> UnsubscribePostAsync(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "unsubscribe")]
		HttpRequest request)
	{
		var queryParameters = request.Query;

		var siteUrl = queryParameters["lang"] == "en" ? AppSettings.SiteEnUrl : AppSettings.SiteFrUrl;
		var redirectionUrl = new Uri(siteUrl + "unsubscribe-complete.html");

		if (IsValid(queryParameters))
		{
			var unsubscribeEntity = ParseUnsubscribeEntity(queryParameters);
			await unsubscribeEntities.AddEntityAsync(unsubscribeEntity);
		}
		else
		{
			redirectionUrl = BuildConfirmationUrl(queryParameters);
		}

		request.HttpContext.Response.Headers.Append("Location", redirectionUrl.ToString());
		return new StatusCodeResult(StatusCodes.Status303SeeOther);
	}

	[Function("UnsubscribeGet")]
	public async Task<IActionResult> UnsubscribeGetAsync(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "unsubscribe")]
		HttpRequest request)
	{
		var queryParameters = request.Query;

		var redirectionUrl = BuildConfirmationUrl(queryParameters);

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
			origin = string.IsNullOrEmpty(queryParameters["origin"]) ? "post" : queryParameters["origin"],
			lang = queryParameters["lang"],
		};

	private static Uri BuildConfirmationUrl(IQueryCollection queryParameters)
	{
		var baseUrl = queryParameters["lang"] == "en" ? AppSettings.SiteEnUrl : AppSettings.SiteFrUrl;

		var confirmationUrl = new UriBuilder(baseUrl + "unsubscribe.html");
		var confirmationParameters = HttpUtility.ParseQueryString(string.Empty);
		confirmationParameters.Add("user", queryParameters["user"]);
		confirmationParameters.Add("email", queryParameters["email"]);
		confirmationParameters.Add("id", queryParameters["id"]);
		confirmationParameters.Add("reason", queryParameters["reason"]);
		confirmationParameters.Add("origin", queryParameters["origin"]);
		confirmationUrl.Query = confirmationParameters.ToString() ?? string.Empty;

		return confirmationUrl.Uri;
	}
}
