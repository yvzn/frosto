using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using batch.Models;

namespace batch.Services;

internal static class EnglishFormatter
{
	internal static readonly CultureInfo EnglishCultureInfo = CultureInfo.CreateSpecificCulture("en-US");

	public static string FormatSubject(List<Forecast> forecasts, LocationEntity location)
	{
		var header = "Temperatures close to zero forecast for the coming days";

		var forecastsBelow0 = forecasts.Where(f => f.Minimum < 0).ToArray();
		if (forecastsBelow0.Length != 0)
		{
			var first = forecastsBelow0.OrderBy(f => f.Date).First();
			header = string.Format(
				EnglishCultureInfo,
				"Freezing temperatures forecast for {0:dddd, MMMM d}: {1}°",
				first.Date,
				first.Minimum
			);
		}

		var forecastsBelowThreshold = location.minThreshold.HasValue
			? [.. forecasts.Where(f => f.Minimum <= Convert.ToDecimal(location.minThreshold.Value))]
			: Array.Empty<Forecast>();
		if (forecastsBelowThreshold.Length != 0)
		{
			var first = forecastsBelowThreshold.OrderBy(f => f.Date).First();
			header = string.Format(
				EnglishCultureInfo,
				"Temperatures below {0}° forecast for {1:dddd, MMMM d}: {2}°",
				location.minThreshold,
				first.Date,
				first.Minimum
			);
		}

		return header;
	}
}

internal static class EnglishHtmlFormatter
{
	private static readonly string tableHeaderTemplate = "<table><thead><tr><th>date<th>minimum<th>maximum<th></thead><tbody>";

	private static readonly string tableRowTemplate = "<tr><td>{0:dddd, MMMM d}<td>{1}° {2}<td>{3}°<td>{4}</tr>";

	private static readonly string tableFooterTemplate = "</tbody></table>";

	private static readonly string messageTemplate =
	@"<p>Hello,

<p>The temperature forecast for the coming days ({0}, {1}):

{2}

<p>Best regards,
<br>Yvan from FrostAlert.net

<p>To stop receiving these messages,
<|unsubscribe_link|>
reply ""STOP"" to this message.

<hr>

<p>Weather data is provided by <em>Open-Meteo.com</em> &mdash;
<a href=""https://open-meteo.com/"" target=""_blank"" rel=""noopener noreferrer"">Weather data by Open-Meteo.com</a>";

	private static readonly string unsubscribeLinkTemplate = @"use this <a href=""{0}"" target=""_blank"">unsubscribe link</a> or";

	public static string FormatBody(List<Forecast> forecasts, LocationEntity location)
	{
		var table = new StringBuilder();
		table.Append(tableHeaderTemplate);
		table.Append(Environment.NewLine);
		var previousMinimum = decimal.MinValue;

		foreach (var forecast in forecasts.OrderBy(f => f.Date))
		{
			table.Append(string.Format(
				EnglishFormatter.EnglishCultureInfo,
				tableRowTemplate,
				forecast.Date,
				forecast.Minimum,
				forecast.Minimum < 0 ? '❄' : ' ',
				forecast.Maximum,
				forecast.Minimum < previousMinimum ? "dropping" : " "
			));
			table.Append(Environment.NewLine);

			previousMinimum = forecast.Minimum;
		}

		table.Append(tableFooterTemplate);

		return string.Format(
				EnglishFormatter.EnglishCultureInfo,
				messageTemplate,
				location.city,
				location.country,
				table.ToString()
			);
	}

	public static string FormatUnsubscribeLink(string unsubscribeUrl)
	{
		return string.Format(
			unsubscribeLinkTemplate,
			unsubscribeUrl
		);
	}
}

internal static class EnglishTextFormatter
{
	private static readonly string tableRowTemplate = "{0,-15:ddd MMM dd}   {1,-6:N1}{2,-1}   {3,-6:N1}   {4}";

	private static readonly string textTemplate =
	@"Hello,

The temperature forecast for the coming days ({0}, {1}):

{2}

Best regards,
Yvan from FrostAlert.net

To unsubscribe, reply ""STOP"" to this message.

__________

Weather data is provided by Open-Meteo.com -- Weather data by Open-Meteo.com";

	public static string FormatBody(List<Forecast> forecasts, LocationEntity location)
	{
		var table = new StringBuilder();
		table.Append(string.Format(
			EnglishFormatter.EnglishCultureInfo,
			tableRowTemplate,
			"date", "min", "", "max", ""
 		));
		table.Append(Environment.NewLine);
		var previousMinimum = decimal.MinValue;

		foreach (var forecast in forecasts.OrderBy(f => f.Date))
		{
			table.Append(string.Format(
				EnglishFormatter.EnglishCultureInfo,
				tableRowTemplate,
				forecast.Date,
				forecast.Minimum,
				forecast.Minimum < 0 ? '❄' : ' ',
				forecast.Maximum,
				forecast.Minimum < previousMinimum ? "dropping" : " "
			));
			table.Append(Environment.NewLine);

			previousMinimum = forecast.Minimum;
		}

		return string.Format(
				EnglishFormatter.EnglishCultureInfo,
				textTemplate,
				location.city,
				location.country,
				table.ToString()
			);
	}
}
