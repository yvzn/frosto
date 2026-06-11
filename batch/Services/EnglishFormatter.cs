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

	internal static bool IsFahrenheit(LocationEntity location)
		=> "F".Equals(location.temperatureUnit, StringComparison.OrdinalIgnoreCase);

	internal static decimal ToDisplayTemperature(decimal celsius, bool fahrenheit)
		=> fahrenheit ? Math.Round(celsius * 9m / 5m + 32m, 0) : celsius;

	public static string FormatSubject(List<weather.Forecast> forecasts, LocationEntity location)
	{
		var fahrenheit = IsFahrenheit(location);
		var unit = fahrenheit ? "F" : "C";

		var header = "Temperatures close to zero forecast for the coming days";

		var forecastsBelow0 = forecasts.Where(f => f.Minimum < 0).ToArray();
		if (forecastsBelow0.Length != 0)
		{
			var first = forecastsBelow0.OrderBy(f => f.Date).First();
			header = string.Format(
				EnglishCultureInfo,
				"Freezing temperatures forecast for {0:dddd, MMMM d}: {1}°{2}",
				first.Date,
				ToDisplayTemperature(first.Minimum, fahrenheit),
				unit
			);
		}

		var forecastsBelowThreshold = location.minThreshold.HasValue
			? [.. forecasts.Where(f => f.Minimum <= Convert.ToDecimal(location.minThreshold.Value))]
			: Array.Empty<weather.Forecast>();
		if (forecastsBelowThreshold.Length != 0)
		{
			var first = forecastsBelowThreshold.OrderBy(f => f.Date).First();
			var thresholdDisplay = fahrenheit
				? ToDisplayTemperature(Convert.ToDecimal(location.minThreshold!.Value), fahrenheit)
				: (object?)location.minThreshold;
			header = string.Format(
				EnglishCultureInfo,
				"Temperatures below {0}°{1} forecast for {2:dddd, MMMM d}: {3}°{1}",
				thresholdDisplay,
				unit,
				first.Date,
				ToDisplayTemperature(first.Minimum, fahrenheit)
			);
		}

		return header;
	}
}

internal static class EnglishHtmlFormatter
{
	private static readonly string tableHeaderTemplate = "<table><thead><tr><th>date<th>minimum<th>maximum<th></thead><tbody>";

	private static readonly string tableRowTemplate = "<tr><td>{0:dddd, MMMM d}<td>{1}°{2} {3}<td>{4}°{2}<td>{5}</tr>";

	private static readonly string tableFooterTemplate = "</tbody></table>";

	private static readonly string messageTemplate =
	@"<p>Hello,

<p>The temperature forecast for the coming days ({0}, {1}):

{2}{3}

<p>Best regards,
<br>Yvan from FrostAlert.net

<p>To stop receiving these messages,
<|unsubscribe_link|>
reply ""STOP"" to this e-mail.

<p>To change the temperature unit, reply with ""CELSIUS"" or ""FAHRENHEIT"" to this e-mail.

<hr>

<p>Weather data is provided by <em>Open-Meteo.com</em> &mdash;
<a href=""https://open-meteo.com/"" target=""_blank"" rel=""noopener noreferrer"">Weather data by Open-Meteo.com</a>";

	private static readonly string applicationInviteTemplate =
	@"

<p>You can also visit our application to <a href=""{0}"" target=""_blank"">add the alerts to your calendar</a>.";

	private static readonly string unsubscribeLinkTemplate = @"use this <a href=""{0}"" target=""_blank"">unsubscribe link</a> or";

	public static string FormatBody(List<weather.Forecast> forecasts, LocationEntity location)
	{
		var fahrenheit = EnglishFormatter.IsFahrenheit(location);
		var unit = fahrenheit ? "F" : "C";

		var table = new StringBuilder();
		table.Append(tableHeaderTemplate);
		table.Append(Environment.NewLine);
		var previousMinimum = decimal.MinValue;

		foreach (var forecast in forecasts.OrderBy(f => f.Date))
		{
			var displayMin = EnglishFormatter.ToDisplayTemperature(forecast.Minimum, fahrenheit);
			var displayMax = EnglishFormatter.ToDisplayTemperature(forecast.Maximum, fahrenheit);
			table.Append(string.Format(
				EnglishFormatter.EnglishCultureInfo,
				tableRowTemplate,
				forecast.Date,
				displayMin,
				unit,
				forecast.Minimum < 0 ? '❄' : ' ',
				displayMax,
				forecast.Minimum < previousMinimum ? "dropping" : " "
			));
			table.Append(Environment.NewLine);

			previousMinimum = forecast.Minimum;
		}

		table.Append(tableFooterTemplate);

		var applicationInvite = "";
		if (location.appEnabled is true)
		{
			applicationInvite = string.Format(
				EnglishFormatter.EnglishCultureInfo,
				applicationInviteTemplate,
				$"{AppSettings.SiteEnUrl}app/weather-forecast/{location.PartitionKey}/{location.RowKey}"
			);
		}

		return string.Format(
				EnglishFormatter.EnglishCultureInfo,
				messageTemplate,
				location.city,
				location.country,
				table.ToString(),
				applicationInvite
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
	private static readonly string tableRowTemplate = "{0,-15:ddd MMM dd}   {1,-8:N1}{2,-2}   {3,-8:N1}{4,-2}   {5}";

	private static readonly string textTemplate =
	@"Hello,

The temperature forecast for the coming days ({0}, {1}):

{2}{3}

Best regards,
Yvan from FrostAlert.net

To change the temperature unit, reply with ""CELSIUS"" or ""FAHRENHEIT"" to this e-mail.

To stop receiving these messages, reply ""STOP"" to this e-mail.

__________

Weather data is provided by Open-Meteo.com -- Weather data by Open-Meteo.com";

	public static string FormatBody(List<weather.Forecast> forecasts, LocationEntity location)
	{
		var fahrenheit = EnglishFormatter.IsFahrenheit(location);
		var unit = fahrenheit ? "°F" : "°C";

		var table = new StringBuilder();
		table.Append(string.Format(
			EnglishFormatter.EnglishCultureInfo,
			tableRowTemplate,
			"date", "min", unit, "max", unit, ""
 		));
		table.Append(Environment.NewLine);
		var previousMinimum = decimal.MinValue;

		foreach (var forecast in forecasts.OrderBy(f => f.Date))
		{
			var displayMin = EnglishFormatter.ToDisplayTemperature(forecast.Minimum, fahrenheit);
			var displayMax = EnglishFormatter.ToDisplayTemperature(forecast.Maximum, fahrenheit);
			table.Append(string.Format(
				EnglishFormatter.EnglishCultureInfo,
				tableRowTemplate,
				forecast.Date,
				displayMin,
				forecast.Minimum < 0 ? '❄' : ' ',
				displayMax,
				' ',
				forecast.Minimum < previousMinimum ? "dropping" : " "
			));
			table.Append(Environment.NewLine);

			previousMinimum = forecast.Minimum;
		}

		var applicationInvite = new StringBuilder();
		if (location.appEnabled is true)
		{
			applicationInvite.AppendLine();
			applicationInvite.AppendLine("You can also visit our application to add the alerts to your calendar:");
			applicationInvite.Append($"{AppSettings.SiteEnUrl}app/weather-forecast/{location.PartitionKey}/{location.RowKey}");
		}

		return string.Format(
				EnglishFormatter.EnglishCultureInfo,
				textTemplate,
				location.city,
				location.country,
				table.ToString(),
				applicationInvite.ToString()
			);
	}
}
