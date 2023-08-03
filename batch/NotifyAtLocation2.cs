using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Azure.Data.Tables;
using batch.Models;
using batch.Services;
using System.Net;

namespace batch;

public static class NotifyAtLocation2
{
#if true //DEBUG
	private static readonly decimal threshold = 20.0m;
#else
	private static readonly decimal threshold = 1.0m;
#endif

	private static HttpClient httpClient = new();

	[FunctionName("NotifyAtLocation2")]
	public static async Task<IActionResult> RunAsync(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
		HttpRequest req,
		[Table("validlocation", Connection = "ALERTS_CONNECTION_STRING")]
		TableClient tableClient,
		ILogger log)
	{
		var entityKey = Decode(req);
		if (entityKey is null || !IsValid(entityKey))
		{
			log.LogError("Failed to decode location p={PartitionKey} r={RowKey}", req.Query["p"], req.Query["r"]);
			return new BadRequestResult();
		}

		var query = () => tableClient.GetEntityAsync<LocationEntity>(entityKey.PartitionKey, entityKey.RowKey);
		var location = await RetryPolicy.ForDataAccessAsync.ExecuteAsync(query);

		var response = location.GetRawResponse();
		if (response.IsError)
		{
			log.LogError("Failed to get location {PartitionKey} {RowKey} {StatusCode} [{ResponseContent}]", entityKey.PartitionKey, entityKey.RowKey, response.Status, Encoding.UTF8.GetString(response.Content));
			return new BadRequestResult();
		}

		try
		{
			await NotifyAtLocationAsync(location.Value, log);
			return new OkResult();
		}
		catch (Exception)
		{
			return new StatusCodeResult(StatusCodes.Status502BadGateway);
		}
	}

	private static EntityKey? Decode(HttpRequest req)
		=> new()
		{
			PartitionKey = req.Query["p"],
			RowKey = req.Query["r"],
		};

	private static bool IsValid(EntityKey entityKey)
		=> !string.IsNullOrWhiteSpace(entityKey.PartitionKey)
			&& !string.IsNullOrWhiteSpace(entityKey.RowKey);

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
			var subject = Formatter.FormatSubject(forecastsBelowThreshold);
			var body = HtmlFormatter.FormatBody(forecastsBelowThreshold, location);
			var text = TextFormatter.FormatBody(forecastsBelowThreshold, location);
			var notification = new Notification
			{
				subject = subject,
				body = body,
				raw = text,
				to = users
			};


			string channel = "default";
			if (SendNotification2.channels.Contains(location.channel ?? "") && !string.IsNullOrEmpty(location.channel))
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

		var response = default(HttpResponseMessage);

		try
		{
			var request = () => httpClient.GetAsync(requestUri);
			response = await RetryPolicy.ForExternalHttpAsync.ExecuteAsync(request);
		}
		catch (Exception ex)
		{
			var responseContent = response is null ? "<empty response>" : await response.Content.ReadAsStringAsync();
			log.LogError(ex, "Failed to get weather for {Coordinates}: HTTP {StatusCode} {RequestUri} [{ResponseContent}]", coordinates, response?.StatusCode, requestUri, responseContent);
			throw;
		}

		if (!response.IsSuccessStatusCode)
		{
			var responseContent = response is null ? "<empty response>" : await response.Content.ReadAsStringAsync();
			log.LogError("Failed to get weather for {Coordinates}: HTTP {StatusCode} {RequestUri} [{ResponseContent}]", coordinates, response?.StatusCode, requestUri, responseContent);
			throw new Exception(string.Format("Failed to get weather for {0}: HTTP {1} {2} [{3}]", coordinates, response?.StatusCode, requestUri, responseContent));
		}

		var weatherApiResult = default(WeatherApiResult);

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

	private static async Task ScheduleNotificationAsync(Notification notification, string channel, ILogger log)
	{
		var users = string.Join(" ", notification.to);

		log.LogInformation("Scheduling notification to <{Users}> on {ChannelName} channel", users, channel);

		var requestUri = $"http://{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}/api/SendNotification2?c={channel}";

		var response = default(HttpResponseMessage);

		try
		{
			var request = () => httpClient.PostAsJsonAsync(requestUri, notification);
			response = await RetryPolicy.ForInternalHttpAsync.ExecuteAsync(request);

			if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.BadGateway)
			{
				var responseContent = response is null ? "<empty response>" : await response.Content.ReadAsStringAsync();
				log.LogError("Failed to schedule notification to {Users}: HTTP {StatusCode} {RequestUri} [{ResponseContent}]", users, response?.StatusCode, requestUri, responseContent);
				throw new Exception(string.Format("Failed to schedule notification to {0}: HTTP {1} {2} [{3}]", users, response?.StatusCode, requestUri, responseContent));
			}
		}
		catch (Exception ex)
		{
			log.LogError(ex, "Failed to schedule notification to {Users}: HTTP {StatusCode} {RequestUri}", users, response?.StatusCode, requestUri);
			throw;
		}
	}
}
