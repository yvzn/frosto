using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using batch.Models;
using batch.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace batch;

public class LocationLoop2
{
	private static readonly Random random = new();

	private static readonly HttpClient httpClient = new();

	[FunctionName("LocationLoop2-0")]
	public static async Task RunGroup0Async(
		[TimerTrigger("0 0 4 * 1-5,9-12 *"
#if DEBUG
			, RunOnStartup=true
#endif
		)]
		TimerInfo timerInfo,
		ILogger log)
	{
#if DEBUG
		await Task.Delay(5_000);
#endif

		_ = LoopOverBatchAsync(groupNumber: 0, log);
	}

	[FunctionName("LocationLoop2-1")]
	public static void RunGroup1(
		[TimerTrigger("0 30 4 * 1-5,9-12 *")]
		TimerInfo timerInfo,
		ILogger log)
	{
		_ = LoopOverBatchAsync(groupNumber: 1, log);
	}

	[FunctionName("LocationLoop2-2")]
	public static void RunGroup2(
		[TimerTrigger("0 0 5 * 1-5,9-12 *")]
		TimerInfo timerInfo,
		ILogger log)
	{
		_ = LoopOverBatchAsync(groupNumber: 2, log);
	}

	[FunctionName("LocationLoop2-3")]
	public static void RunGroup3(
		[TimerTrigger("0 30 5 * 1-5,9-12 *")]
		TimerInfo timerInfo,
		ILogger log)
	{
		_ = LoopOverBatchAsync(groupNumber: 3, log);
	}

	private static async Task LoopOverBatchAsync(int groupNumber, ILogger log)
	{
		var dayNumber = (DateTime.UtcNow - DateTime.UnixEpoch).Days % AppSettings.PeriodInDays;
		await LoopOverBatchAsync(dayNumber, groupNumber, log);
	}

	private static async Task LoopOverBatchAsync(int dayNumber, int groupNumber, ILogger log)
	{
		var partitionKey = $"day-{dayNumber}";
		var rowKey = $"day-{dayNumber}-group-{groupNumber}";

		log.LogInformation("Scheduling batch {BatchRowKey} for weather", rowKey);

		var tableClient = new TableClient(AppSettings.AlertsConnectionString, "batch");

		async ValueTask<Azure.NullableResponse<BatchEntity>> query(CancellationToken cancellationToken) => await tableClient.GetEntityIfExistsAsync<BatchEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		var batchEntity = await RetryStrategy.For.DataAccess.Execute(query);

		if (batchEntity.HasValue)
		{
			LoopOverBatch(batchEntity.Value, log);
		}
		else
		{
			log.LogInformation("Skipping batch {DayNumber}-{GroupNumber} because it does not exist", dayNumber, groupNumber);
		}
	}

	private static void LoopOverBatch(BatchEntity batchEntity, ILogger log)
	{
		var validLocationIds = batchEntity.locations?.Split(' ');
		if (validLocationIds is null || validLocationIds.Length is 0)
		{
			log.LogWarning("Skipping batch {BatchRowKey} because it has no locations", batchEntity.RowKey);
			return;
		}

		int locationIndex = -1;
		foreach (var locationId in validLocationIds)
		{
			var (partitionKey, rowKey) = locationId.ToKeys();
			if (partitionKey is null || rowKey is null)
			{
				log.LogWarning("Skipping location {LocationId} in batch {BatchRowKey} because of invalid identifier", locationId, batchEntity.RowKey);
				continue;
			}

			Interlocked.Increment(ref locationIndex);
			_ = ScheduleLocationAsync(partitionKey, rowKey, locationIndex, log);
		}
	}

	private static async Task<bool> ScheduleLocationAsync(string partitionKey, string rowKey, int locationIndex, ILogger log)
	{
		var tableClient = new TableClient(AppSettings.AlertsConnectionString, "validlocation");

		async ValueTask<Azure.NullableResponse<LocationEntity>> query(CancellationToken cancellationToken) => await tableClient.GetEntityIfExistsAsync<LocationEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		var locationEntity = await RetryStrategy.For.DataAccess.Execute(query);

		Func<Azure.NullableResponse<LocationEntity>, bool> locationFilter = location => location.HasValue;

#if DEBUG
		locationFilter = location => location.HasValue && location.Value.uat == true;
#endif

		if (locationFilter.Invoke(locationEntity))
		{
			return await ScheduleLocationAsync(locationEntity.Value, locationIndex, log);
		}
		else
		{
			log.LogWarning("Skipping location {LocationPartitionKey} {LocationRowKey} because it does not exist", partitionKey, rowKey);
			return false;
		}
	}

	private static async Task<bool> ScheduleLocationAsync(LocationEntity location, int locationIndex, ILogger log)
	{
		var users = location.users?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (users is null || users.Length == 0)
		{
			log.LogWarning("Skipping location {City} {Country} because no user configured", location.city, location.country);
			return false;
		}

		log.LogInformation("Scheduling location {City} {Country} for weather", location.city, location.country);

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
				log.LogError("Failed to schedule location {City} {Country} for weather: HTTP {StatusCode} {RequestUri}", location.city, location.country, response.StatusCode, requestUri.AbsoluteUri);
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			log.LogError(ex, "Failed to schedule location {City} {Country} for weather: HTTP {StatusCode} {RequestUri}", location.city, location.country, response?.StatusCode, requestUri.AbsoluteUri);
			return false;
		}
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