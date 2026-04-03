using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using batch.Models;

namespace batch.Services;

internal static class FrenchFormatter
{
	internal static readonly CultureInfo FrenchCultureInfo = CultureInfo.CreateSpecificCulture("fr-FR");

	public static string FormatSubject(List<weather.Forecast> forecasts, LocationEntity location)
	{
		var header = "Températures proches de zéro prévues ces prochains jours";

		var forecastsBelow0 = forecasts.Where(f => f.Minimum < 0).ToArray();
		if (forecastsBelow0.Length != 0)
		{
			var first = forecastsBelow0.OrderBy(f => f.Date).First();
			header = string.Format(
				FrenchCultureInfo,
				"Températures négatives prévues {0:dddd d MMMM}: {1}°",
				first.Date,
				first.Minimum
			);
		}

		var forecastsBelowThreshold = location.minThreshold.HasValue
			? [.. forecasts.Where(f => f.Minimum <= Convert.ToDecimal(location.minThreshold.Value))]
			: Array.Empty<weather.Forecast>();
		if (forecastsBelowThreshold.Length != 0)
		{
			var first = forecastsBelowThreshold.OrderBy(f => f.Date).First();
			header = string.Format(
				FrenchCultureInfo,
				"Températures en dessous de {0}° prévues {1:dddd d MMMM}: {2}°",
				location.minThreshold,
				first.Date,
				first.Minimum
			);
		}

		return header;
	}
}

internal static class FrenchHtmlFormatter
{
	private static readonly string tableHeaderTemplate = "<table><thead><tr><th>date<th>minimum<th>maximum<th></thead><tbody>";

	private static readonly string tableRowTemplate = "<tr><td>{0:dddd d MMMM}<td>{1}° {2}<td>{3}°<td>{4}</tr>";

	private static readonly string tableFooterTemplate = "</tbody></table>";

	private static readonly string messageTemplate =
	@"<p>Bonjour,

<p>Les prévisions de température des prochains jours ({0}, {1}):

{2}{3}

<p>Cordialement,
<br>Yvan de Alertegelee.fr

<p>Pour ne plus recevoir ces messages,
<|unsubscribe_link|>
répondez ""STOP"" à ce message.

<hr>

<p>Les données météo sont fournies par <em>Open-Meteo.com</em> &mdash;
<a href=""https://open-meteo.com/"" target=""_blank"" rel=""noopener noreferrer"">Weather data by Open-Meteo.com</a>";

	private static readonly string applicationInviteTemplate =
	@"

<p>Vous pouvez également visiter notre application pour <a href=""{0}"" target=""_blank"">ajouter les alertes à votre calendrier</a>.";

	internal static readonly string unsubscribeLinkPlaceholder = "<|unsubscribe_link|>";

	private static readonly string unsubscribeLinkTemplate = @"utilisez ce <a href=""{0}"" target=""_blank"">lien de désinscription</a> ou";

	public static string FormatBody(List<weather.Forecast> forecasts, LocationEntity location)
	{
		var table = new StringBuilder();
		table.Append(tableHeaderTemplate);
		table.Append(Environment.NewLine);
		var previousMinimum = decimal.MinValue;

		foreach (var forecast in forecasts.OrderBy(f => f.Date))
		{
			table.Append(string.Format(
				FrenchFormatter.FrenchCultureInfo,
				tableRowTemplate,
				forecast.Date,
				forecast.Minimum,
				forecast.Minimum < 0 ? '❄' : ' ',
				forecast.Maximum,
				forecast.Minimum < previousMinimum ? "en baisse" : " "
			));
			table.Append(Environment.NewLine);

			previousMinimum = forecast.Minimum;
		}

		table.Append(tableFooterTemplate);

		var applicationInvite = "";
		if (location.appEnabled is true)
		{
			applicationInvite = string.Format(
				FrenchFormatter.FrenchCultureInfo,
				applicationInviteTemplate,
				$"{AppSettings.SiteFrUrl}app/weather-forecast/{location.PartitionKey}/{location.RowKey}"
			);
		}

		return string.Format(
				FrenchFormatter.FrenchCultureInfo,
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

internal static class TextFormatter
{
	private static readonly string tableRowTemplate = "{0,-12:ddd dd MMM}   {1,-6:N1}{2,-1}   {3,-6:N1}   {4}";

	private static readonly string textTemplate =
	@"Bonjour,

Les prévisions de température des prochains jours ({0}, {1}):

{2}{3}

Cordialement,
Yvan de Alertegelee.fr

Pour vous désinscrire, répondez ""STOP"" à ce message.

__________

Les données météo sont fournies par Open-Meteo.com -- Weather data by Open-Meteo.com";

	public static string FormatBody(List<weather.Forecast> forecasts, LocationEntity location)
	{
		var table = new StringBuilder();
		table.Append(string.Format(
			FrenchFormatter.FrenchCultureInfo,
			tableRowTemplate,
			"date", "mini", "", "maxi", ""
 		));
		table.Append(Environment.NewLine);
		var previousMinimum = decimal.MinValue;

		foreach (var forecast in forecasts.OrderBy(f => f.Date))
		{
			table.Append(string.Format(
				FrenchFormatter.FrenchCultureInfo,
				tableRowTemplate,
				forecast.Date,
				forecast.Minimum,
				forecast.Minimum < 0 ? '❄' : ' ',
				forecast.Maximum,
				forecast.Minimum < previousMinimum ? "en baisse" : " "
			));
			table.Append(Environment.NewLine);

			previousMinimum = forecast.Minimum;
		}

		var applicationInvite = new StringBuilder();
		if (location.appEnabled is true)
		{
			applicationInvite.AppendLine();
			applicationInvite.AppendLine("Vous pouvez également visiter notre application pour ajouter les alertes à votre calendrier:");
			applicationInvite.Append($"{AppSettings.SiteFrUrl}app/weather-forecast/{location.PartitionKey}/{location.RowKey}");
		}

		return string.Format(
				FrenchFormatter.FrenchCultureInfo,
				textTemplate,
				location.city,
				location.country,
				table.ToString(),
				applicationInvite.ToString()
			);
	}
}
