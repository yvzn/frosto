using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
using System.Threading;
using Microsoft.Azure.Functions.Worker;
using System.Net.Http.Json;
using Microsoft.Extensions.Azure;
using System.Globalization;

namespace batch;

public class NotifyAtLocation2(IHttpClientFactory httpClientFactory, IAzureClientFactory<TableClient> azureClientFactory, ILogger<NotifyAtLocation2> logger)
{
#if DEBUG
	private static readonly decimal threshold = 20.0m;
#else
	private static readonly decimal threshold = 1.0m;
#endif

	private readonly HttpClient httpClient = httpClientFactory.CreateClient();

	private readonly TableClient validLocationTableClient = azureClientFactory.CreateClient("validlocationTableClient");

	[Function("NotifyAtLocation2")]
	public async Task<IActionResult> RunAsync(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
		HttpRequest req)
	{
		var entityKey = Decode(req);
		if (entityKey is null || !IsValid(entityKey))
		{
			logger.LogError("Failed to decode location p={PartitionKey} r={RowKey}", req.Query["p"], req.Query["r"]);
			return new BadRequestResult();
		}

		async ValueTask<Azure.Response<LocationEntity>> query(CancellationToken cancellationToken) => await validLocationTableClient.GetEntityAsync<LocationEntity>(entityKey.PartitionKey, entityKey.RowKey, cancellationToken: cancellationToken);
		var location = await RetryStrategy.For.DataAccess.ExecuteAsync(query);

		var response = location.GetRawResponse();
		if (response.IsError)
		{
			logger.LogError("Failed to get location {PartitionKey} {RowKey} {StatusCode} [{ResponseContent}]", entityKey.PartitionKey, entityKey.RowKey, response.Status, Encoding.UTF8.GetString(response.Content));
			return new BadRequestResult();
		}

		try
		{
			await NotifyAtLocationAsync(location.Value);
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

	private async Task NotifyAtLocationAsync(LocationEntity location)
	{
		var users = location.users?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (users is null || users.Length == 0)
		{
			logger.LogWarning("Skipping location {City} {Country} because no user configured", location.city, location.country);
			return;
		}

		var forecasts = await GetWeatherForecastsAsync(location.coordinates);

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
			if (!string.IsNullOrEmpty(location.channel) && SendNotification2.channels.Contains(location.channel))
			{
				channel = location.channel;
			}

			await ScheduleNotificationAsync(notification, channel);
		}
	}

	private async Task<IList<Forecast>?> GetWeatherForecastsAsync(string? coordinates)
	{
		logger.LogInformation("Get weather for {Coordinates}", coordinates);

		if (coordinates is null) throw new ArgumentNullException(nameof(coordinates));

		var (latitude, longitude) = ParseCoordinates(coordinates);
		if (!latitude.HasValue || !longitude.HasValue) throw new ArgumentOutOfRangeException(nameof(coordinates), coordinates, "Expected comma-separated numbers");

		var requestUri = string.Format(CultureInfo.InvariantCulture, AppSettings.WeatherApiUrl, latitude.Value, longitude.Value);

		var response = default(HttpResponseMessage);

		try
		{
			async ValueTask<HttpResponseMessage> request(CancellationToken cancellationToken) => await httpClient.GetAsync(requestUri, cancellationToken);
			response = await RetryStrategy.For.ExternalHttp.ExecuteAsync(request);
		}
		catch (Exception ex)
		{
			var responseContent = response is null ? "<empty response>" : await response.Content.ReadAsStringAsync();
			logger.LogError(ex, "Failed to get weather for {Coordinates}: HTTP {StatusCode} {RequestUri} [{ResponseContent}]", coordinates, response?.StatusCode, requestUri, responseContent);
			throw;
		}

		if (!response.IsSuccessStatusCode)
		{
			var responseContent = response is null ? "<empty response>" : await response.Content.ReadAsStringAsync();
			logger.LogError("Failed to get weather for {Coordinates}: HTTP {StatusCode} {RequestUri} [{ResponseContent}]", coordinates, response?.StatusCode, requestUri, responseContent);
			throw new Exception(string.Format("Failed to get weather for {0}: HTTP {1} {2} [{3}]", coordinates, response?.StatusCode, requestUri, responseContent));
		}

		var weatherApiResult = default(WeatherApiResult);

		try
		{
			weatherApiResult = await JsonSerializer.DeserializeAsync<WeatherApiResult>(await response.Content.ReadAsStreamAsync());
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to parse weather forecast for {Coordinates}", coordinates);
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

		if (split.Length > 0 && decimal.TryParse(split[0], CultureInfo.InvariantCulture, out var parsedAt0))
		{
			latitude = parsedAt0;
		}
		if (split.Length > 1 && decimal.TryParse(split[1], CultureInfo.InvariantCulture, out var parsedAt1))
		{
			longitude = parsedAt1;
		}

		return (latitude, longitude);
	}

	private async Task ScheduleNotificationAsync(Notification notification, string channel)
	{
		var users = string.Join(" ", notification.to);

		logger.LogInformation("Scheduling notification to <{Users}> on {ChannelName} channel", users, channel);

		var requestUri = new InternalRequestUri("SendNotification2", new() { { "c", channel } });

		var response = default(HttpResponseMessage);

		try
		{
			async ValueTask<HttpResponseMessage> request(CancellationToken cancellationToken) => await httpClient.PostAsJsonAsync(requestUri.AbsoluteUri, notification, cancellationToken);
			response = await RetryStrategy.For.InternalHttp.ExecuteAsync(request);

			if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.BadGateway)
			{
				var responseContent = response is null ? "<empty response>" : await response.Content.ReadAsStringAsync();
				logger.LogError("Failed to schedule notification to {Users}: HTTP {StatusCode} {RequestUri} [{ResponseContent}]", users, response?.StatusCode, requestUri.AbsoluteUri, responseContent);
				throw new Exception(string.Format("Failed to schedule notification to {0}: HTTP {1} {2} [{3}]", users, response?.StatusCode, requestUri.AbsoluteUri, responseContent));
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to schedule notification to {Users}: HTTP {StatusCode} {RequestUri}", users, response?.StatusCode, requestUri.AbsoluteUri);
			throw;
		}
	}
}
