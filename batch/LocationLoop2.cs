using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using batch.Models;
using batch.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace batch;

public class LocationLoop2(IHttpClientFactory httpClientFactory, IAzureClientFactory<TableClient> azureClientFactory, ILogger<LocationLoop2> logger)
{
	private static readonly Random random = new();

	private readonly HttpClient httpClient = httpClientFactory.CreateClient("default");

	private readonly TableClient batchTableClient = azureClientFactory.CreateClient("batchTableClient");

	private readonly TableClient validLocationTableClient = azureClientFactory.CreateClient("validlocationTableClient");

	[Function("LocationLoop2-0")]
	public async Task RunGroup0Async(
		[TimerTrigger("0 2 2 * 1-5,9-12 *"
#if DEBUG
			, RunOnStartup=true
#endif
		)]
		TimerInfo timerInfo)
	{
#if DEBUG
		await Task.Delay(10_000);
#else
		await Task.CompletedTask;
#endif

		_ = LoopOverBatchAsync(groupNumber: 0);
	}

	[Function("LocationLoop2-1")]
	public void RunGroup1(
		[TimerTrigger("0 17 2 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 1);
	}

	[Function("LocationLoop2-2")]
	public void RunGroup2(
		[TimerTrigger("0 32 2 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 2);
	}

	[Function("LocationLoop2-3")]
	public void RunGroup3(
		[TimerTrigger("0 47 2 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 3);
	}

	[Function("LocationLoop2-4")]
	public void RunGroup4(
		[TimerTrigger("0 2 3 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 4);
	}

	[Function("LocationLoop2-5")]
	public void RunGroup5(
		[TimerTrigger("0 17 3 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 5);
	}

	[Function("LocationLoop2-6")]
	public void RunGroup6(
		[TimerTrigger("0 32 3 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 6);
	}

	[Function("LocationLoop2-7")]
	public void RunGroup7(
		[TimerTrigger("0 47 3 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 7);
	}

	[Function("LocationLoop2-8")]
	public void RunGroup8(
		[TimerTrigger("0 2 4 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 8);
	}

	[Function("LocationLoop2-9")]
	public void RunGroup9(
		[TimerTrigger("0 17 4 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 9);
	}

	[Function("LocationLoop2-10")]
	public void RunGroup10(
		[TimerTrigger("0 32 4 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 10);
	}

	[Function("LocationLoop2-11")]
	public void RunGroup11(
		[TimerTrigger("0 47 4 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 11);
	}

	[Function("LocationLoop2-12")]
	public void RunGroup12(
		[TimerTrigger("0 2 5 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 12);
	}

	[Function("LocationLoop2-13")]
	public void RunGroup13(
		[TimerTrigger("0 17 5 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 13);
	}

	[Function("LocationLoop2-14")]
	public void RunGroup14(
		[TimerTrigger("0 32 5 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 14);
	}

	[Function("LocationLoop2-15")]
	public void RunGroup15(
		[TimerTrigger("0 47 5 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 15);
	}

	[Function("LocationLoop2-16")]
	public void RunGroup16(
		[TimerTrigger("0 2 6 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 16);
	}

	[Function("LocationLoop2-17")]
	public void RunGroup17(
		[TimerTrigger("0 17 6 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 17);
	}

	[Function("LocationLoop2-18")]
	public void RunGroup18(
		[TimerTrigger("0 32 6 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 18);
	}

	[Function("LocationLoop2-19")]
	public void RunGroup19(
		[TimerTrigger("0 47 6 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 19);
	}

	[Function("LocationLoop2-20")]
	public void RunGroup20(
		[TimerTrigger("0 2 7 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 20);
	}

	[Function("LocationLoop2-21")]
	public void RunGroup21(
		[TimerTrigger("0 17 7 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 21);
	}

	[Function("LocationLoop2-22")]
	public void RunGroup22(
		[TimerTrigger("0 32 7 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 22);
	}

	[Function("LocationLoop2-23")]
	public void RunGroup23(
		[TimerTrigger("0 47 7 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 23);
	}

	[Function("LocationLoop2-24")]
	public void RunGroup24(
		[TimerTrigger("0 2 8 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 24);
	}

	[Function("LocationLoop2-25")]
	public void RunGroup25(
		[TimerTrigger("0 17 8 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 25);
	}

	[Function("LocationLoop2-26")]
	public void RunGroup26(
		[TimerTrigger("0 32 8 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 26);
	}

	[Function("LocationLoop2-27")]
	public void RunGroup27(
		[TimerTrigger("0 47 8 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 27);
	}

	[Function("LocationLoop2-28")]
	public void RunGroup28(
		[TimerTrigger("0 2 9 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 28);
	}

	[Function("LocationLoop2-29")]
	public void RunGroup29(
		[TimerTrigger("0 17 9 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 29);
	}

	[Function("LocationLoop2-30")]
	public void RunGroup30(
		[TimerTrigger("0 32 9 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 30);
	}

	[Function("LocationLoop2-31")]
	public void RunGroup31(
		[TimerTrigger("0 47 9 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 31);
	}

	[Function("LocationLoop2-32")]
	public void RunGroup32(
		[TimerTrigger("0 2 10 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 32);
	}

	[Function("LocationLoop2-33")]
	public void RunGroup33(
		[TimerTrigger("0 17 10 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 33);
	}

	[Function("LocationLoop2-34")]
	public void RunGroup34(
		[TimerTrigger("0 32 10 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 34);
	}

	[Function("LocationLoop2-35")]
	public void RunGroup35(
		[TimerTrigger("0 47 10 * 1-5,9-12 *")]
		TimerInfo timerInfo)
	{
		_ = LoopOverBatchAsync(groupNumber: 35);
	}

	private async Task LoopOverBatchAsync(int groupNumber)
	{
		var dayNumber = (DateTime.UtcNow - DateTime.UnixEpoch).Days % AppSettings.PeriodInDays;
		await LoopOverBatchAsync(dayNumber, groupNumber);
	}

	private async Task LoopOverBatchAsync(int dayNumber, int groupNumber)
	{
		var partitionKey = $"day-{dayNumber}";
		var rowKey = $"day-{dayNumber}-group-{groupNumber}";

		logger.LogInformation("Scheduling batch {BatchRowKey} for weather", rowKey);

		async ValueTask<Azure.NullableResponse<BatchEntity>> query(CancellationToken cancellationToken) => await batchTableClient.GetEntityIfExistsAsync<BatchEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		var batchEntity = await RetryStrategy.For.DataAccess.Execute(query);

		if (batchEntity.HasValue)
		{
			LoopOverBatch(batchEntity.Value!);
		}
		else
		{
			logger.LogInformation("Skipping batch {DayNumber}-{GroupNumber} because it does not exist", dayNumber, groupNumber);
		}
	}

	private void LoopOverBatch(BatchEntity batchEntity)
	{
		var validLocationIds = batchEntity.locations?.Split(' ');
		if (validLocationIds is null || validLocationIds.Length is 0)
		{
			logger.LogWarning("Skipping batch {BatchRowKey} because it has no locations", batchEntity.RowKey);
			return;
		}

		int locationIndex = -1;
		foreach (var locationId in validLocationIds)
		{
			var (partitionKey, rowKey) = locationId.ToKeys();
			if (partitionKey is null || rowKey is null)
			{
				logger.LogWarning("Skipping location {LocationId} in batch {BatchRowKey} because of invalid identifier", locationId, batchEntity.RowKey);
				continue;
			}

			Interlocked.Increment(ref locationIndex);
			_ = ScheduleLocationAsync(partitionKey, rowKey, locationIndex);
		}
	}

	private async Task<bool> ScheduleLocationAsync(string partitionKey, string rowKey, int locationIndex)
	{
		async ValueTask<Azure.NullableResponse<LocationEntity>> query(CancellationToken cancellationToken) => await validLocationTableClient.GetEntityIfExistsAsync<LocationEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		var locationEntity = await RetryStrategy.For.DataAccess.Execute(query);

		Func<Azure.NullableResponse<LocationEntity>, bool> locationFilter = location => location.HasValue && location.Value?.disabled != true;

#if DEBUG
		locationFilter = location => location.HasValue && location.Value?.uat == true && location.Value?.disabled != true;
#endif

		if (locationFilter.Invoke(locationEntity))
		{
			return await ScheduleLocationAsync(locationEntity.Value!, locationIndex);
		}
		else
		{
			logger.LogWarning("Skipping location {LocationPartitionKey} {LocationRowKey} because it does not exist", partitionKey, rowKey);
			return false;
		}
	}

	private async Task<bool> ScheduleLocationAsync(LocationEntity location, int locationIndex)
	{
		var users = location.users?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (users is null || users.Length == 0)
		{
			logger.LogWarning("Skipping location {City} {Country} because no user configured", location.city, location.country);
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
