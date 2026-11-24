using System.Collections.Specialized;
using System.Net.Mail;
using System.Web;
using Azure.Data.Tables;
using Frosto.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using support.Model;

namespace support;

public class Feedback
{
	private readonly TableClient feedbackEntities;

	public Feedback(IAzureClientFactory<TableClient> azureClientFactory)
	{
		feedbackEntities = azureClientFactory.CreateClient("feedbackTableClient");
#if DEBUG
		_ = feedbackEntities.CreateIfNotExistsAsync();
#endif
	}

	[Function("Feedback")]
	public async Task<IActionResult> PostAsync(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "feedback")]
		HttpRequest request)
	{
		var body = await new StreamReader(request.Body).ReadToEndAsync();
		var parameters = HttpUtility.ParseQueryString(body);

		if (!IsValid(parameters))
		{
			return new BadRequestResult();
		}

		var entity = new FeedbackEntity
		{
			PartitionKey = nameof(FeedbackEntity),
			RowKey = Guid.NewGuid().ToString(),
			sentiment = parameters["sentiment"]?.ToLowerInvariant(),
			text = ParseText(parameters["text"]),
			email = string.IsNullOrWhiteSpace(parameters["email"]) ? null : parameters["email"]?.Trim(),
			consent = string.Equals(parameters["consent"], "true", StringComparison.OrdinalIgnoreCase),
			lang = parameters["lang"],
			source = parameters["source"] == "alert-email" ? "alert-email" : "home",
			locationPartitionKey = ParseOptionalLocationKey(parameters["locationPartitionKey"]),
			locationRowKey = ParseOptionalLocationKey(parameters["locationRowKey"]),
			duration = ParseDuration(parameters["duration"]),
		};

		await feedbackEntities.AddEntityAsync(entity);

		var lang = entity.lang == "en" ? "en" : "fr";
		var siteUrl = lang == "en" ? AppSettings.SiteEnUrl : AppSettings.SiteFrUrl;
		request.HttpContext.Response.Headers.Append("Location", siteUrl + "feedback-thank-you.html");
		return new StatusCodeResult(StatusCodes.Status303SeeOther);
	}

	private static bool IsValid(NameValueCollection parameters)
	{
		var sentiment = parameters["sentiment"];
		if (sentiment is not ("positive" or "negative"))
		{
			return false;
		}

		var lang = parameters["lang"];
		if (lang is not ("fr" or "en"))
		{
			return false;
		}

		var source = parameters["source"];
		if (source is not (null or "" or "home" or "alert-email"))
		{
			return false;
		}

		var text = parameters["text"];
		if (text is { Length: > 500 })
		{
			return false;
		}

		var email = parameters["email"];
		if (!string.IsNullOrWhiteSpace(email))
		{
			if (email.Length > 254
				|| !IsValidEmail(email)
				|| !string.Equals(parameters["consent"], "true", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}
		else if (string.Equals(parameters["consent"], "true", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		var partitionKey = parameters["locationPartitionKey"];
		var rowKey = parameters["locationRowKey"];
		if ((string.IsNullOrWhiteSpace(partitionKey) != string.IsNullOrWhiteSpace(rowKey))
			|| !string.IsNullOrEmpty(partitionKey) && !IsValidLocationKey(partitionKey)
			|| !string.IsNullOrEmpty(rowKey) && !IsValidLocationKey(rowKey))
		{
			return false;
		}

		if (!string.IsNullOrWhiteSpace(parameters["comment"]))
		{
			return false;
		}

		var duration = ParseDuration(parameters["duration"]);
		return !duration.HasValue || duration.Value >= 500;
	}

	private static bool IsValidEmail(string email)
	{
		try
		{
			_ = new MailAddress(email);
			return email == email.Trim();
		}
		catch
		{
			return false;
		}
	}

	private static bool IsValidLocationKey(string? value)
		=> value is { Length: > 0 and <= 128 }
			&& value.IndexOfAny(['/', '\\', '#', '?']) < 0
			&& !value.Any(char.IsControl);

	private static string? ParseOptionalLocationKey(string? value)
		=> string.IsNullOrWhiteSpace(value) ? null : value;

	private static string? ParseText(string? value)
		=> string.IsNullOrWhiteSpace(value) ? null
			: value.Length > 500 ? value[..500] : value;

	private static int? ParseDuration(string? value)
		=> int.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var duration)
			&& duration >= 0
			? duration
			: null;
}
