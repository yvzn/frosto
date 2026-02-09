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
		var queryParameters = await GetQueryParameters(request);

		Uri redirectionUrl;

		if (IsValid(queryParameters))
		{
			var unsubscribeEntity = ParseUnsubscribeEntity(queryParameters);
			await unsubscribeEntities.AddEntityAsync(unsubscribeEntity);

			var siteUrl = GetSiteUrl(queryParameters);
			redirectionUrl = new Uri(siteUrl + "unsubscribe-complete.html");
		}
		else
		{
			redirectionUrl = BuildConfirmationUrl(queryParameters, "post_invalid");
		}

		request.HttpContext.Response.Headers.Append("Location", redirectionUrl.ToString());
		return new StatusCodeResult(StatusCodes.Status303SeeOther);
	}

	[Function("UnsubscribeGet")]
	public async Task<IActionResult> UnsubscribeGetAsync(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "unsubscribe")]
		HttpRequest request)
	{
		var queryParameters = await GetQueryParameters(request);

		var redirectionUrl = BuildConfirmationUrl(queryParameters, "get");

		request.HttpContext.Response.Headers.Append("Location", redirectionUrl.ToString());
		return new StatusCodeResult(StatusCodes.Status303SeeOther);
	}

	private async static Task<UnsubscribeQueryParameters> GetQueryParameters(HttpRequest request)
	{
		var result = new UnsubscribeQueryParameters();

		// parameters can be sent either via query string or form data, we need to read both and combine them

		var queryStringParameters = request.Query;
		foreach (var key in queryStringParameters.Keys)
		{
			result[key] = [.. queryStringParameters[key]];
		}

		var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
		var formDataParameters = HttpUtility.ParseQueryString(requestBody);

		foreach (var key in formDataParameters.AllKeys)
		{
			if (key is null) continue;

			var formValues = formDataParameters.GetValues(key);
			if (formValues is null) continue;

			if (result.TryGetValue(key, out string?[]? queryStringValues))
			{
				result[key] = [.. queryStringValues, .. formValues];
			}
			else
			{
				result[key] = [.. formValues];
			}
		}

		return result;
	}

	private static bool IsValid(UnsubscribeQueryParameters queryParameters)
		=> (IsTokenValid(queryParameters) || IsEmailValid(queryParameters))
		&& IsLangValid(queryParameters);

	private static bool IsTokenValid(UnsubscribeQueryParameters queryParameters)
		=> new[] { "token", "user" }
		.Any(key => queryParameters.TryGetValue(key, out var values)
			&& values.Length == 1
			&& values.All(v => v?.StartsWith("ey") is true));

	private static bool IsEmailValid(UnsubscribeQueryParameters queryParameters)
		=> queryParameters.TryGetValue("email", out var emailValues)
			&& emailValues.Length == 1
			&& emailValues.All(v => !string.IsNullOrWhiteSpace(v));

	private static bool IsLangValid(UnsubscribeQueryParameters queryParameters)
		=> queryParameters.TryGetValue("lang", out var langValues)
			&& langValues.Length == 1
			&& langValues.All(v => v == "en" || v == "fr");

	private static UnsubscribeEntity ParseUnsubscribeEntity(UnsubscribeQueryParameters queryParameters)
		=> new()
		{
			PartitionKey = nameof(UnsubscribeEntity),
			RowKey = Guid.NewGuid().ToString(),
			token = queryParameters.FirstOrDefault("token"),
			user = queryParameters.FirstOrDefault("user"),
			email = queryParameters.FirstOrDefault("email"),
			locid = queryParameters.FirstOrDefault("locid"),
			reason = queryParameters.FirstOrDefault("reason"),
			origin = queryParameters.FirstOrDefault("origin", defaultValue: "post"),
			lang = queryParameters.FirstOrDefault("lang"),
		};

	private static Uri BuildConfirmationUrl(UnsubscribeQueryParameters queryParameters, string origin)
	{
		var baseUrl = GetSiteUrl(queryParameters);

		var confirmationUrl = new UriBuilder(baseUrl + "unsubscribe.html");
		var confirmationParameters = HttpUtility.ParseQueryString(string.Empty);
		confirmationParameters.Add("user", queryParameters.FirstOrDefault("user"));
		confirmationParameters.Add("email", queryParameters.FirstOrDefault("email"));
		confirmationParameters.Add("locid", queryParameters.FirstOrDefault("locid"));
		confirmationParameters.Add("reason", queryParameters.FirstOrDefault("reason"));
		confirmationParameters.Add("origin", queryParameters.FirstOrDefault("origin", defaultValue: origin));
		confirmationUrl.Query = confirmationParameters.ToString() ?? string.Empty;

		return confirmationUrl.Uri;
	}

	private static string GetSiteUrl(UnsubscribeQueryParameters queryParameters)
		=> queryParameters.TryGetValue("lang", out var langValues) && langValues.Contains("en")
			? AppSettings.SiteEnUrl
			: AppSettings.SiteFrUrl;
}

internal class UnsubscribeQueryParameters : Dictionary<string, string?[]>
{
	public string? FirstOrDefault(string key, string? defaultValue = default)
	{
		if (TryGetValue(key, out var values) && values.Length > 0)
		{
			return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
		}
		return defaultValue;
	}
}
