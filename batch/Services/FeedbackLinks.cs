using System;
using batch.Models;

namespace batch.Services;

internal static class FeedbackLinks
{
	private const string Placeholder = "<|feedback_links|>";

	public static string FormatHtml(SingleRecipientNotification notification)
	{
		if (string.IsNullOrWhiteSpace(notification.partitionKey) || string.IsNullOrWhiteSpace(notification.rowKey))
		{
			return string.Empty;
		}

		var en = notification.lang == "en";
		var baseUrl = en ? AppSettings.SiteEnUrl : AppSettings.SiteFrUrl;
		var positive = BuildUrl(baseUrl, "positive", notification.partitionKey, notification.rowKey);
		var negative = BuildUrl(baseUrl, "negative", notification.partitionKey, notification.rowKey);
		return en
			? $"<p>Was this alert useful? <a href=\"{positive}\" target=\"_blank\" rel=\"noopener noreferrer\">👍 Yes</a> · <a href=\"{negative}\" target=\"_blank\" rel=\"noopener noreferrer\">👎 No</a></p>"
			: $"<p>Cette alerte vous a-t-elle été utile ? <a href=\"{positive}\" target=\"_blank\" rel=\"noopener noreferrer\">👍 Oui</a> · <a href=\"{negative}\" target=\"_blank\" rel=\"noopener noreferrer\">👎 Non</a></p>";
	}

	public static string FormatText(SingleRecipientNotification notification)
	{
		if (string.IsNullOrWhiteSpace(notification.partitionKey) || string.IsNullOrWhiteSpace(notification.rowKey))
		{
			return string.Empty;
		}

		var en = notification.lang == "en";
		var baseUrl = en ? AppSettings.SiteEnUrl : AppSettings.SiteFrUrl;
		var positive = BuildUrl(baseUrl, "positive", notification.partitionKey, notification.rowKey);
		var negative = BuildUrl(baseUrl, "negative", notification.partitionKey, notification.rowKey);
		return en
			? $"Was this alert useful? 👍 Yes: {positive} · 👎 No: {negative}"
			: $"Cette alerte vous a-t-elle été utile ? 👍 Oui : {positive} · 👎 Non : {negative}";
	}

	public static string ReplaceHtml(string? body, SingleRecipientNotification notification)
		=> (body ?? string.Empty).Replace(Placeholder, FormatHtml(notification));

	public static string ReplaceText(string? text, SingleRecipientNotification notification)
		=> (text ?? string.Empty).Replace(Placeholder, FormatText(notification));

	private static string BuildUrl(string baseUrl, string sentiment, string partitionKey, string rowKey)
	{
		var parameters = System.Web.HttpUtility.ParseQueryString(string.Empty);
		parameters["s"] = sentiment;
		parameters["p"] = partitionKey;
		parameters["r"] = rowKey;
		return baseUrl + "feedback.html?" + parameters;
	}
}
