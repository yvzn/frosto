using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using api.Data;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace api;

public static class NotifyAtLocation
{
	private static readonly decimal threshold = 10.0m;

	private static HttpClient httpClient = new();

	private static IDictionary<string, QueueClient> queueClientByChannel = BuildQueueClients();

	[FunctionName("NotifyAtLocation")]
	public static async Task RunAsync(
		[QueueTrigger("location-loop", Connection = "ALERTS_CONNECTION_STRING")]
		QueueMessage queueMessage,
		[Table("validlocation", Connection = "ALERTS_CONNECTION_STRING")]
		TableClient tableClient,
		ILogger log)
	{
		var entityKey = Decode(queueMessage);
		if (entityKey is null)
		{
			log.LogError("Failed to decode location {EntityKey}", (string?)queueMessage.Body.ToString());
			throw new Exception(string.Format("Failed to decode location {0}", (string?)queueMessage.Body.ToString()));
		}

		var location = await tableClient.GetEntityAsync<LocationEntity>(entityKey.PartitionKey, entityKey.RowKey);
		var response = location.GetRawResponse();
		if (response.IsError)
		{
			log.LogError("Failed to get location {PartitionKey} {RowKey}", entityKey.PartitionKey, entityKey.RowKey, response.Status, Encoding.UTF8.GetString(response.Content));
			throw new Exception(string.Format("Failed to get location {0} {1}", entityKey.PartitionKey, entityKey.RowKey, response.Status, Encoding.UTF8.GetString(response.Content)));
		}

		await NotifyAtLocationAsync(location.Value, log);
	}

	private static EntityKey? Decode(QueueMessage queueMessage)
	{
		var base64 = queueMessage.Body.ToString();
		var bytes = Convert.FromBase64String(base64);
		var json = Encoding.UTF8.GetString(bytes);

		return JsonSerializer.Deserialize<EntityKey>(json);
	}

	private static async Task NotifyAtLocationAsync(LocationEntity location, ILogger log)
	{
		var users = location.users?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (users is null || users.Length == 0)
		{
			log.LogWarning("Skipping location {City} {Country} because no user configured", location.city, location.country);
			return;
		}

		var forecasts = await GetWeatherForecastsAsync(location.coordinates, log);

		var forecastsBelowThreshold = forecasts?.Where(f => f.Minimum <= threshold).ToList();
		if (forecastsBelowThreshold?.Any() is true)
		{
			var subject = FormatNotificationSubject(forecastsBelowThreshold);
			var body = FormatNotificationBody(forecastsBelowThreshold, location);
			var notification = new Notification
			{
				subject = subject,
				body = body,
				to = users
			};

			string channel = "default";
			if (queueClientByChannel.ContainsKey(location.channel ?? "") && !string.IsNullOrWhiteSpace(location.channel))
			{
				channel = location.channel;
			}

			await ScheduleNotificationAsync(notification, channel, log);
		}
	}

	private static async Task<IList<Forecast>?> GetWeatherForecastsAsync(string? coordinates, ILogger log)
	{
		log.LogInformation("Get weather for {Coordinates}", coordinates);

		if (coordinates is null) throw new ArgumentNullException(nameof(coordinates));

		var (latitude, longitude) = ParseCoordinates(coordinates);
		if (!latitude.HasValue || !longitude.HasValue) throw new ArgumentOutOfRangeException(nameof(coordinates), coordinates, "Expected comma-separated numbers");

		var requestUri = string.Format(AppSettings.WeatherApiUrl, latitude.Value, longitude.Value);

		var response = default(HttpResponseMessage?);

		try
		{
			response = await httpClient.GetAsync(requestUri);
		}
		catch (Exception ex)
		{
			var responseContent = response is null ? "<empty response>" : await response.Content.ReadAsStringAsync();
			log.LogError(ex, "Failed to get weather for {Coordinates}: HTTP {StatusCode} {RequestUri} [{ResponseContent}]", coordinates, response?.StatusCode, requestUri, responseContent);
			throw;
		}

		if (!response.IsSuccessStatusCode)
		{
			throw new Exception(string.Format("Failed to get weather for {0}: HTTP {1} {2} [{3}]", coordinates, response.StatusCode, requestUri, await response.Content.ReadAsStringAsync()));
		}

		var weatherApiResult = default(WeatherApiResult?);

		try
		{
			weatherApiResult = await JsonSerializer.DeserializeAsync<WeatherApiResult>(await response.Content.ReadAsStreamAsync());
		}
		catch (Exception ex)
		{
			log.LogError(ex, "Failed to parse weather forecast for {Coordinates}", coordinates);
			throw;
		}

		return weatherApiResult?.daily.time
			.Zip(
				weatherApiResult?.daily.temperature_2m_min ?? Array.Empty<decimal>(),
				weatherApiResult?.daily.temperature_2m_max ?? Array.Empty<decimal>())
			.Select(tuple => new Forecast(
				DateOnly.FromDateTime(tuple.First),
				tuple.Second,
				tuple.Third))
			.ToList();
	}

	private static (decimal? latitude, decimal? longitude) ParseCoordinates(string coordinates)
	{
		decimal? latitude = default;
		decimal? longitude = default;

		var split = coordinates.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		if (split.Length > 0 && decimal.TryParse(split[0], out var parsedAt0))
		{
			latitude = parsedAt0;
		}
		if (split.Length > 1 && decimal.TryParse(split[1], out var parsedAt1))
		{
			longitude = parsedAt1;
		}

		return (latitude, longitude);
	}

	private static readonly CultureInfo cultureInfo = CultureInfo.CreateSpecificCulture("fr-FR");

	private static string FormatNotificationSubject(List<Forecast> forecasts)
	{
		var header = "Températures proches de zéro prévues ces prochains jours";
		var forecastsBelow0 = forecasts.Where(f => f.Minimum < 0).ToList();
		if (forecastsBelow0.Any())
		{
			var first = forecastsBelow0.OrderBy(f => f.Date).First();
			header = string.Format(
				cultureInfo,
				"Températures négatives prévues {0:dddd d MMMM}: {1}°",
				first.Date,
				first.Minimum
			);
		}

		return header;
	}

	private static readonly string tableHeaderTemplate = "<table><thead><tr><th>date<th>minimum<th>maximum<th></thead><tbody>";

	private static readonly string tableRowTemplate = "<tr><td>{0:dddd d MMMM}<td>{1}° {2}<td>{3}°<td>{4}</tr>";

	private static readonly string tableFooterTemplate = "</tbody></table>";

	private static readonly string notificationTemplate =
	@"<p>Bonjour,

<p>Les prévisions de température des prochains jours ({0}, {1}):

{2}

<p>Cordialement,
<br>L'équipe Alertegelee.fr

<p>Pour vous désinscrire, répondez ""STOP"" à ce message.

<hr>

<p>Les données météo sont fournies par <em>Open-Meteo.com</em> &mdash;
<a href=""https://open-meteo.com/"" target=""_blank"" rel=""noopener noreferrer"">Weather data by Open-Meteo.com</a>";

	private static string FormatNotificationBody(List<Forecast> forecasts, LocationEntity location)
	{
		var table = new StringBuilder();
		table.Append(tableHeaderTemplate);
		table.Append(Environment.NewLine);
		var previousMinimum = decimal.MinValue;

		foreach (var forecast in forecasts.OrderBy(f => f.Date))
		{
			table.Append(string.Format(
				cultureInfo,
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

		return string.Format(
				cultureInfo,
				notificationTemplate,
				location.city,
				location.country,
				table.ToString()
			);
	}

	private static IDictionary<string, QueueClient> BuildQueueClients()
	{
		return new[] { "default", "tipimail", "smtp" }
			.ToDictionary(
				channel => channel,
				channel => new QueueClient(AppSettings.AlertsConnectionString, $"email-{channel}", new() { MessageEncoding = QueueMessageEncoding.Base64 }));
	}

	private static async Task<bool> ScheduleNotificationAsync(Notification notification, string channel, ILogger log)
	{
		var users = string.Join(" ", notification.to);

		log.LogInformation("Scheduling notification to <{Users}> on {ChannelName} channel", users, channel);

		var json = JsonSerializer.Serialize(notification);
		var base64 = EncodeBase64(json);

		var queueClient = queueClientByChannel[channel];
		Response<SendReceipt> response = await queueClient.SendMessageAsync(base64);

		if (response.GetRawResponse().IsError)
		{
			throw new Exception(string.Format("Failed to schedule notification to {0}: {1} {2}", users, response.GetRawResponse().Status, response.GetRawResponse().ReasonPhrase));
		}
		return true;
	}

	private static string EncodeBase64(string json)
		=> Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
}
