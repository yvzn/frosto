using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using api.Data;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace api;

public class WeatherForecast
{
	private readonly TableClient validLocationEntities;
	private readonly HttpClient httpClient;
	private readonly ILogger<WeatherForecast> logger;

	public WeatherForecast(
		IAzureClientFactory<TableClient> azureClientFactory,
		IHttpClientFactory httpClientFactory,
		ILogger<WeatherForecast> logger)
	{
		validLocationEntities = azureClientFactory.CreateClient("validlocationTableClient");
		httpClient = httpClientFactory.CreateClient("default");
		this.logger = logger;

#if DEBUG
		_ = Task.WhenAll(
			validLocationEntities.CreateIfNotExistsAsync()
		);
#endif
	}

	[Function("WeatherForecast")]
	public async Task<IActionResult> Run(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "weather-forecast")]
		HttpRequest request,
		CancellationToken cancellationToken)
	{
		var queryParameters = request.Query;
		if (!IsValid(queryParameters))
		{
			logger.LogWarning("Failed to decode location p={PartitionKey} r={RowKey}", queryParameters["p"], queryParameters["r"]);
			return new BadRequestResult();
		}

		var (PartitionKey, RowKey) = Decode(queryParameters);

		Azure.Response<LocationEntity> location;

		try
		{
			location = await validLocationEntities.GetEntityAsync<LocationEntity>(PartitionKey, RowKey, cancellationToken: cancellationToken);
		}
		catch (Azure.RequestFailedException ex) when (ex.Status == 404)
		{
			logger.LogWarning(ex, "Failed to get location {PartitionKey} {RowKey}", PartitionKey, RowKey);
			return new BadRequestResult();
		}

		var locationResponse = location.GetRawResponse();
		if (locationResponse.IsError)
		{
			logger.LogError("Failed to get location {PartitionKey} {RowKey} {StatusCode} [{ResponseContent}]", PartitionKey, RowKey, locationResponse.Status, Encoding.UTF8.GetString(locationResponse.Content));
			return new BadRequestResult();
		}

		var requestUri = weather.RequestUri.From(location.Value, AppSettings.WeatherApiUrl);

		var response = default(HttpResponseMessage);

		try
		{
			response = await httpClient.GetAsync(requestUri, cancellationToken);
		}
		catch (Exception ex)
		{
			var responseContent = response is null ? "<empty response>" : await response.Content.ReadAsStringAsync(cancellationToken);
			logger.LogError(ex, "Failed to get weather for {Coordinates}: HTTP {StatusCode} {RequestUri} [{ResponseContent}]", location.Value.coordinates, response?.StatusCode, requestUri, responseContent);
			return new StatusCodeResult(StatusCodes.Status502BadGateway);
		}

		if (!response.IsSuccessStatusCode)
		{
			var responseContent = response is null ? "<empty response>" : await response.Content.ReadAsStringAsync(cancellationToken);
			logger.LogError("Failed to get weather for {Coordinates}: HTTP {StatusCode} {RequestUri} [{ResponseContent}]", location.Value.coordinates, response?.StatusCode, requestUri, responseContent);
			return new StatusCodeResult(StatusCodes.Status502BadGateway);
		}

		weather.OpenMeteoApiResult? weatherApiResult;

		try
		{
			weatherApiResult = await JsonSerializer.DeserializeAsync<weather.OpenMeteoApiResult>(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to parse weather forecast for {Coordinates}", location.Value.coordinates);
			return new StatusCodeResult(StatusCodes.Status502BadGateway);
		}

		var forecasts = new weather.ForecastBuilder(
			weatherApiResult, location.Value, applyTemperatureThreshold: false).Build();

		if (forecasts is not null)
		{
			return new OkObjectResult(new
			{
				location = new
				{
					location.Value.city,
					location.Value.country,
					temperatureThreshold = location.Value.minThreshold.HasValue is true
						? Convert.ToDecimal(location.Value.minThreshold.Value)
						: weather.Forecast.defaultTemperatureThreshold,
				},
				forecasts
			});
		}

		logger.LogError("Weather forecast for {Coordinates} has no data [{RequestUri}]", location.Value.coordinates, requestUri);
		return new StatusCodeResult(StatusCodes.Status502BadGateway);
	}

	private static (string PartitionKey, string RowKey) Decode(IQueryCollection queryParameters)
		=> (
			queryParameters["p"]!,
			queryParameters["r"]!
		);

	private static bool IsValid(IQueryCollection queryParameters)
		=> !string.IsNullOrWhiteSpace(queryParameters["p"])
			&& !string.IsNullOrWhiteSpace(queryParameters["r"]);
}
