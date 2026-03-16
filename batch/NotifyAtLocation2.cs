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
	private static readonly (string, string) FromFrench = ("Yvan de AlerteGelee.fr", "eXZhbkBhbGVydGVnZWxlZXMuZnI=");
	private static readonly (string, string) FromEnglish = ("Yvan from FrostAlert.net", "eXZhbkBmcm9zdGFsZXJ0Lm5ldA==");

	private readonly HttpClient httpClient = httpClientFactory.CreateClient("default");
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

		var forecastsBelowThreshold = await GetWeatherForecastsAsync(location);
		if (forecastsBelowThreshold.Count is 0)
		{
			return;
		}

		var notification = BuildNotification(forecastsBelowThreshold, location);
		notification.to = users;

		string channel = "default";
		if (!string.IsNullOrEmpty(location.channel) && SendNotification2.channels.Contains(location.channel))
		{
			channel = location.channel;
		}

		await ScheduleNotificationAsync(notification, channel);
	}

	private async Task<List<weather.Forecast>> GetWeatherForecastsAsync(LocationEntity location)
	{
		logger.LogInformation("Get weather for {Coordinates}", location.coordinates);

		var requestUri = weather.RequestUri.From(location, AppSettings.WeatherApiUrl);

		var response = default(HttpResponseMessage);

		try
		{
			async ValueTask<HttpResponseMessage> request(CancellationToken cancellationToken) => await httpClient.GetAsync(requestUri, cancellationToken);
			response = await RetryStrategy.For.ExternalHttp.ExecuteAsync(request);
		}
		catch (Exception ex)
		{
			var responseContent = response is null ? "<empty response>" : await response.Content.ReadAsStringAsync();
			logger.LogError(ex, "Failed to get weather for {Coordinates}: HTTP {StatusCode} {RequestUri} [{ResponseContent}]", location.coordinates, response?.StatusCode, requestUri, responseContent);
			throw;
		}

		if (!response.IsSuccessStatusCode)
		{
			var responseContent = response is null ? "<empty response>" : await response.Content.ReadAsStringAsync();
			logger.LogError("Failed to get weather for {Coordinates}: HTTP {StatusCode} {RequestUri} [{ResponseContent}]", location.coordinates, response?.StatusCode, requestUri, responseContent);
			throw new Exception(string.Format("Failed to get weather for {0}: HTTP {1} {2} [{3}]", location.coordinates, response?.StatusCode, requestUri, responseContent));
		}

		var weatherApiResult = default(weather.OpenMeteoApiResult);

		try
		{
			weatherApiResult = await JsonSerializer.DeserializeAsync<weather.OpenMeteoApiResult>(await response.Content.ReadAsStreamAsync());
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to parse weather forecast for {Coordinates}", location.coordinates);
			throw;
		}

		var forecasts = new weather.ForecastBuilder(
			weatherApiResult, location, applyTemperatureThreshold: true).Build();

		if (forecasts is not null)
		{
			return forecasts;
		}

		logger.LogError("Weather forecast for {Coordinates} has no data [{RequestUri}]", location.coordinates, requestUri);
		throw new Exception(string.Format("Weather forecast for {0} has no data [{1}]", location.coordinates, weatherApiResult));
	}

	private static Notification BuildNotification(List<weather.Forecast> forecasts, LocationEntity location)
	{
		// Determine language, default to French
		var language = location.lang ?? "fr";

		var subject = language switch
		{
			"en" => EnglishFormatter.FormatSubject(forecasts, location),
			_ => Formatter.FormatSubject(forecasts, location)
		};

		var body = language switch
		{
			"en" => EnglishHtmlFormatter.FormatBody(forecasts, location),
			_ => HtmlFormatter.FormatBody(forecasts, location)
		};

		var text = language switch
		{
			"en" => EnglishTextFormatter.FormatBody(forecasts, location),
			_ => TextFormatter.FormatBody(forecasts, location)
		};

		var (displayName, address) = language switch
		{
			"en" => FromEnglish,
			_ => FromFrench
		};

		var notification = new Notification
		{
			subject = subject,
			body = body,
			raw = text,
			lang = language,
			rowKey = location.RowKey,
			from = new()
			{
				address = Encoding.UTF8.GetString(Convert.FromBase64String(address)),
				displayName = displayName
			},
		};

		return notification;
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
