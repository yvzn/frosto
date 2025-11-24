using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using admin.Models;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;

namespace admin.Services;

public class BatchService(
	IAzureClientFactory<TableClient> azureClientFactory)
{
	private readonly TableClient _batchTableClient = azureClientFactory.CreateClient("batchTableClient");
	private readonly TableClient _validLocationTableClient = azureClientFactory.CreateClient("validlocationTableClient");

	internal async Task DeleteAllBatches(CancellationToken cancellationToken)
	{
		await foreach (var batchEntity in _batchTableClient.QueryAsync<TableEntity>(select: ["PartitionKey", "RowKey"], cancellationToken: cancellationToken))
		{
			await _batchTableClient.DeleteEntityAsync(batchEntity.PartitionKey, batchEntity.RowKey, cancellationToken: cancellationToken);
		}
	}

	internal async Task<int> CreateBatches(int periodInDays, int batchCountPerDay, CancellationToken cancellationToken)
	{
		var validLocations = await GetValidLocationsAsync(cancellationToken);

		var batchCount = periodInDays * batchCountPerDay;

		var batches = validLocations.AsBatches(batchCount);

		foreach (var batchEntity in batches.ConvertToBatchEntities(periodInDays))
		{
			await _batchTableClient.AddEntityAsync(batchEntity, cancellationToken: cancellationToken);
		}

		return batchCount;
	}

	private async Task<List<LocationEntity>> GetValidLocationsAsync(CancellationToken cancellationToken)
	{
		var results = new List<LocationEntity>();
		await foreach (var validLocationEntity in _validLocationTableClient.QueryAsync<LocationEntity>(select: ["PartitionKey", "RowKey", "offset"], cancellationToken: cancellationToken))
		{
			results.Add(validLocationEntity);
		}

		results.Sort(new LocationEntityOffsetComparer());
		return results;
	}
}

internal static class ListExtensions
{
	public static IEnumerable<ICollection<TValue>> AsBatches<TValue>(this List<TValue> source, int batchCount)
	{
		var itemsPerBatch = (int)Math.Ceiling(source.Count / (double)batchCount);
		var partitions = Partitioner.Create(0, source.Count, itemsPerBatch);
		foreach (var (start, end) in partitions.GetDynamicPartitions())
		{
			yield return source[start..end];
		}
	}

	public static IEnumerable<BatchEntity> ConvertToBatchEntities<TValue>(this IEnumerable<ICollection<TValue>> batches, int periodInDays)
		where TValue : ITableEntity
		=> batches.Select((batch, batchNumber)
			=>
			{
				// try to plan the first batches to be executed in the earliest groups of each day
				// exemple: assuming periodInDays = 3
				// batchNumber | dayNumber | groupNumber
				// 0           | 0         | 0
				// 1           | 1         | 0
				// 2           | 2         | 0
				// 3           | 0         | 1
				// 4           | 1         | 1
				// 5           | 2         | 1
				// 6           | 0         | 2
				// 7           | 1         | 2
				// ...
				int dayNumber = batchNumber % periodInDays;
				int groupNumber = batchNumber / periodInDays;

				var locationIds = batch.Select(loc => (loc.PartitionKey, loc.RowKey).ToId());

				return new BatchEntity
				{
					PartitionKey = $"day-{dayNumber}",
					RowKey = $"day-{dayNumber}-group-{groupNumber}",
					locations = string.Join(' ', locationIds),
				};
			});
}

internal class LocationEntityOffsetComparer : IComparer<LocationEntity>
{
	public int Compare(LocationEntity? x, LocationEntity? y)
	{
		var reversed = -1 * CompareOffsets(x, y);
		return reversed;
	}

	public static int CompareOffsets(LocationEntity? x, LocationEntity? y)
	{
		if (x == null && y == null) return 0;
		if (x == null) return -1;
		if (y == null) return 1;

		// The alphabetic order does not work properly with negative offsets
		// so we need to parse them into minutes and compare the integer values
		var xOffset = Parse(x.offset);
		var yOffset = Parse(y.offset);

		return xOffset.CompareTo(yOffset);
	}

	private static int Parse(string? offset)
	{
		if (string.IsNullOrEmpty(offset))
		{
			// treat empty offset as +00:00
			return 0;
		}

		// Custom parsing of the offset property, as negative sign can be either '-' or '−' (U+2212)
		// Example offsets: "+02:00", "-05:30", "−08:00"
		var sign = offset.StartsWith('-') || offset.StartsWith('−') ? -1 : 1;
		var parts = offset.TrimStart('+', '-', '−').Split(':');

		int hours = 0;
		int minutes = 0;

		if (parts.Length >= 1)
		{
			hours = int.Parse(parts[0]);
		}
		if (parts.Length == 2)
		{
			minutes = int.Parse(parts[1]);
		}

		return sign * (hours * 60 + minutes);
	}
}
