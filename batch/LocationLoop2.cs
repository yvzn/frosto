using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using batch.Models;
using batch.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace batch;

public class LocationLoop2(IHttpClientFactory httpClientFactory, IAzureClientFactory<TableClient> azureClientFactory, ILogger<LocationLoop2> logger)
{
	private static readonly Random random = new();

	private readonly HttpClient httpClient = httpClientFactory.CreateClient("default");

	private readonly TableClient locationBatchTableClient = azureClientFactory.CreateClient("locationbatchTableClient");

	private readonly TableClient validLocationTableClient = azureClientFactory.CreateClient("validlocationTableClient");

	[Function("BatchCronJob")]
	public async Task Run(
		[TimerTrigger("0 */20 * * * *", RunOnStartup = false, UseMonitor = true)]
		TimerInfo timer)
	{
		var now = DateTime.UtcNow;
		int slotIndex = (now.Hour * 60 + now.Minute) / 20;
		int dayOfCycle = (int)(now.Date - AppSettings.EpochDate).TotalDays % AppSettings.PeriodInDays;

		logger.LogInformation("Processing slot {SlotIndex} day {DayOfCycle}", slotIndex, dayOfCycle);

		await LoopOverBatchAsync(slotIndex, dayOfCycle, now);
	}

#if DEBUG
	[Function("BatchCronDebug")]
	public async Task<IActionResult> RunDebug(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
		HttpRequest req)
	{
		var slotIndexParam = req.Query["slotIndex"];
		if (!int.TryParse(slotIndexParam, out var slotIndex))
		{
			return new BadRequestResult();
		}

		var dayIndexParam = req.Query["dayIndex"];
		if (!int.TryParse(dayIndexParam, out var dayIndex))
		{
			return new BadRequestResult();
		}

		logger.LogInformation("Processing slot {SlotIndex} day {DayOfCycle}", slotIndex, dayIndex);

		await LoopOverBatchAsync(slotIndex, dayIndex, DateTime.UtcNow);
		return new OkResult();
	}
#endif

	private async Task LoopOverBatchAsync(int slotIndex, int dayIndex, DateTime utcNow)
	{
		int locationIndex = 0;

		await foreach (var batchEntry in locationBatchTableClient.QueryAsync<LocationBatchEntity>(
			e => e.slot_index == slotIndex && e.day_index == dayIndex))
		{
			if (batchEntry.PartitionKey is null || batchEntry.RowKey is null)
			{
				continue;
			}

			var currentIndex = locationIndex++;
			_ = ScheduleLocationAsync(batchEntry.PartitionKey, batchEntry.RowKey, currentIndex, utcNow);
		}

		if (locationIndex == 0)
		{
			logger.LogInformation("No locations found for slot {SlotIndex} day {DayOfCycle}", slotIndex, dayIndex);
		}
	}

	private async Task<bool> ScheduleLocationAsync(string partitionKey, string rowKey, int locationIndex, DateTime utcNow)
	{
		async ValueTask<Azure.NullableResponse<LocationEntity>> query(CancellationToken cancellationToken) => await validLocationTableClient.GetEntityIfExistsAsync<LocationEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		var locationEntity = await RetryStrategy.For.DataAccess.Execute(query);

		Func<Azure.NullableResponse<LocationEntity>, bool> locationFilter = location => location.HasValue && location.Value?.disabled != true;

#if DEBUG
		locationFilter = location => location.HasValue && location.Value?.uat == true && location.Value?.disabled != true;
#endif

		if (locationFilter.Invoke(locationEntity))
		{
			return await ScheduleLocationAsync(locationEntity.Value!, locationIndex, utcNow);
		}
		else
		{
			logger.LogWarning("Skipping location {LocationPartitionKey} {LocationRowKey} because it does not exist", partitionKey, rowKey);
			return false;
		}
	}

	private async Task<bool> ScheduleLocationAsync(LocationEntity location, int locationIndex, DateTime utcNow)
	{
		var users = location.users?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (users is null || users.Length == 0)
		{
			logger.LogWarning("Skipping location {City} {Country} because no user configured", location.city, location.country);
			return false;
		}

		if (!IsWinterSeason(location, utcNow))
		{
			logger.LogInformation("Skipping {PartitionKey}|{RowKey}: out of season ({Hemisphere}, month {Month})",
				location.PartitionKey, location.RowKey, location.hemisphere, utcNow.Month);
			return false;
		}

		logger.LogInformation("Scheduling location {City} {Country} for weather", location.city, location.country);

		var requestUri = new InternalRequestUri("NotifyAtLocation2", new() { { "p", location.PartitionKey }, { "r", location.RowKey } });

		var response = default(HttpResponseMessage);

		try
		{
			var visibilityTimeout = TimeSpan.FromMilliseconds(1_000 * locationIndex + random.Next(500));
			await Task.Delay(visibilityTimeout, CancellationToken.None);

			async ValueTask<HttpResponseMessage> request(CancellationToken cancellationToken) => await httpClient.GetAsync(requestUri.AbsoluteUri, cancellationToken);
			response = await RetryStrategy.For.InternalHttp.ExecuteAsync(request);

			if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.BadGateway)
			{
				logger.LogError("Failed to schedule location {City} {Country} for weather: HTTP {StatusCode} {RequestUri}", location.city, location.country, response.StatusCode, requestUri.AbsoluteUri);
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to schedule location {City} {Country} for weather: HTTP {StatusCode} {RequestUri}", location.city, location.country, response?.StatusCode, requestUri.AbsoluteUri);
			return false;
		}
	}

	internal static bool IsWinterSeason(LocationEntity location, DateTime utcNow)
	{
		int month = utcNow.Month;
		if (location.hemisphere == "S")
		{
			return AppSettings.SouthernWinterMonths.Contains(month);
		}
		// Default to northern hemisphere if not set
		return AppSettings.NorthernWinterMonths.Contains(month);
	}
}


internal static class LocationExtensions
{
	public static (string? PartitionKey, string? RowKey) ToKeys(this string? id)
	{
		var split = id?.Split('|');
		if (split?.Length is > 1)
		{
			var partitionKey = split[0];
			var rowKey = split[1];
			return (partitionKey, rowKey);
		}
		return (default, default);
	}
}

