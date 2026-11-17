using admin.Models;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace admin.Services;

public class BatchService(
	IAzureClientFactory<TableClient> azureClientFactory,
	ILogger<BatchService> logger)
{
	private readonly TableClient _locationBatchTableClient = azureClientFactory.CreateClient("locationbatchTableClient");
	private readonly TableClient _validLocationTableClient = azureClientFactory.CreateClient("validlocationTableClient");

	private const int SlotsPerDay = 72;

	internal async Task DeleteAllBatches(CancellationToken cancellationToken)
	{
		await foreach (var entity in _locationBatchTableClient.QueryAsync<TableEntity>(select: ["PartitionKey", "RowKey"], cancellationToken: cancellationToken))
		{
			await _locationBatchTableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, cancellationToken: cancellationToken);
		}
	}

	internal async Task<int> CreateBatches(int periodInDays, double capacityGuardMultiplier, CancellationToken cancellationToken)
	{
		var allLocations = await GetValidLocationsAsync(cancellationToken);
		var totalLocations = allLocations.Count;

		if (totalLocations == 0)
		{
			return 0;
		}

		var assignedIds = await GetAssignedLocationIdsAsync(cancellationToken);

		var unassigned = allLocations
			.Where(l => !assignedIds.Contains(l.Id))
			.OrderBy(l => l.Id)
			.ToList();

		// Pre-fill bucket load from existing assignments per day
		var bucketLoads = new int[periodInDays][];
		for (int d = 0; d < periodInDays; d++)
		{
			bucketLoads[d] = new int[SlotsPerDay];
		}

		await foreach (var row in _locationBatchTableClient.QueryAsync<LocationBatchEntity>(
			select: ["PartitionKey", "RowKey", "slot_index", "day_index"],
			cancellationToken: cancellationToken))
		{
			if (row.day_index >= 0 && row.day_index < periodInDays
				&& row.slot_index >= 0 && row.slot_index < SlotsPerDay)
			{
				bucketLoads[row.day_index][row.slot_index]++;
			}
		}

		int capacityLimit = (int)Math.Ceiling((double)totalLocations / (SlotsPerDay * (double)periodInDays));

		int assigned = 0;
		foreach (var location in unassigned)
		{
			var dayIndex = (int)(StableHash(location.Id) % (uint)periodInDays);

			uint jitterHash = StableHash(location.Id + ":slot");
			int jitterMinutes = (int)(jitterHash % 120);
			int targetLocalMinutes = 7 * 60 - jitterMinutes;
			int targetUtcMinutes = targetLocalMinutes - (location.UtcOffsetMinutes ?? 0);
			targetUtcMinutes = ((targetUtcMinutes % 1440) + 1440) % 1440;
			int targetSlotIndex = targetUtcMinutes / 20;

			int? bestSlot = null;
			double bestScore = double.NegativeInfinity;

			// Try offset 0 first, then prefer earlier slots (negative offsets) over later ones
			int[] offsets = [0, -1, +1, -2, +2, -3, -4, -5, -6, -7, -8, -9, -10, -11, -12];
			foreach (int offset in offsets)
			{
				int slot = ((targetSlotIndex + offset) % SlotsPerDay + SlotsPerDay) % SlotsPerDay;

				if (bucketLoads[dayIndex][slot] >= capacityLimit * capacityGuardMultiplier)
					continue;

				double loadScore = 1.0 - ((double)bucketLoads[dayIndex][slot] / capacityLimit);

				if (loadScore > bestScore)
				{
					bestScore = loadScore;
					bestSlot = slot;
				}
			}

			if (bestSlot is null)
			{
				logger.LogWarning(
					"All candidate slots over capacity for location {Id}. " +
					"Consider increasing N. Falling back to least-loaded slot.", location.Id);

				bestSlot = Enumerable.Range(0, SlotsPerDay)
					.OrderBy(s => bucketLoads[dayIndex][s])
					.First();
			}

			var (partitionKey, rowKey) = location.Id.ToKeys();
			var batchEntity = new LocationBatchEntity
			{
				PartitionKey = partitionKey ?? string.Empty,
				RowKey = rowKey ?? string.Empty,
				slot_index = bestSlot.Value,
				day_index = dayIndex,
			};
			await _locationBatchTableClient.UpsertEntityAsync(batchEntity, TableUpdateMode.Replace, cancellationToken: cancellationToken);

			bucketLoads[dayIndex][bestSlot.Value]++;
			assigned++;
		}

		return assigned;
	}

	private async Task<List<LocationWithBatchInfo>> GetValidLocationsAsync(CancellationToken cancellationToken)
	{
		var results = new List<LocationWithBatchInfo>();
		await foreach (var entity in _validLocationTableClient.QueryAsync<LocationEntity>(
			select: ["PartitionKey", "RowKey", "utc_offset_minutes"],
			cancellationToken: cancellationToken))
		{
			results.Add(new LocationWithBatchInfo(
				(entity.PartitionKey, entity.RowKey).ToId(),
				entity.utc_offset_minutes));
		}
		return results;
	}

	private async Task<HashSet<string>> GetAssignedLocationIdsAsync(CancellationToken cancellationToken)
	{
		var ids = new HashSet<string>();
		await foreach (var entity in _locationBatchTableClient.QueryAsync<TableEntity>(
			select: ["PartitionKey", "RowKey"],
			cancellationToken: cancellationToken))
		{
			ids.Add((entity.PartitionKey, entity.RowKey).ToId());
		}
		return ids;
	}

	internal async Task<BatchStats> GetBatchStatsAsync(CancellationToken cancellationToken)
	{
		const int slotsPerDay = 72;
		const int slotMinutes = 20;
		const int sampleSize = 5;

		// Load location info keyed by "PartitionKey|RowKey"
		var locationLookup = new Dictionary<string, (string? City, string? Country, string? Hemisphere, int? UtcOffset)>();
		await foreach (var entity in _validLocationTableClient.QueryAsync<LocationEntity>(
			select: ["PartitionKey", "RowKey", "city", "country", "hemisphere", "utc_offset_minutes"],
			cancellationToken: cancellationToken))
		{
			locationLookup[(entity.PartitionKey, entity.RowKey).ToId()] =
				(entity.city, entity.country, entity.hemisphere, entity.utc_offset_minutes);
		}

		// Load all batch assignments
		var batchEntries = new List<LocationBatchEntity>();
		await foreach (var entry in _locationBatchTableClient.QueryAsync<LocationBatchEntity>(
			cancellationToken: cancellationToken))
		{
			batchEntries.Add(entry);
		}

		var stats = new BatchStats { Total = batchEntries.Count };

		foreach (var e in batchEntries)
		{
			if (e.slot_index >= 0 && e.slot_index < slotsPerDay)
				stats.SlotCounts[e.slot_index]++;

			stats.DayCounts[e.day_index] = stats.DayCounts.GetValueOrDefault(e.day_index) + 1;
		}

		foreach (var entry in batchEntries)
		{
			locationLookup.TryGetValue((entry.PartitionKey, entry.RowKey).ToId(), out var loc);

			if (loc.UtcOffset is null)
				stats.MissingOffset++;

			int utcOffset = loc.UtcOffset ?? 0;
			int slotUtcMinutes = entry.slot_index * slotMinutes;
			int localMinutes = ((slotUtcMinutes + utcOffset) % 1440 + 1440) % 1440;

			// Check if local time is between 4:00 and 7:00
			if (localMinutes >= 240 && localMinutes < 420)
			{
				stats.InTimeWindow++;
			}
			else
			{
				stats.OutOfWindow.Add(new OutOfWindowLocation(
					loc.City ?? "(unknown)",
					loc.Country ?? entry.PartitionKey ?? "",
					entry.slot_index,
					localMinutes));
			}
		}

		// Random sample
		var rng = new Random();
		foreach (var entry in batchEntries.OrderBy(_ => rng.Next()).Take(sampleSize))
		{
			locationLookup.TryGetValue((entry.PartitionKey, entry.RowKey).ToId(), out var loc);
			stats.RandomSamples.Add(new BatchSampleLocation(
				loc.City ?? "(unknown)",
				loc.Country ?? entry.PartitionKey ?? "",
				loc.Hemisphere ?? "?",
				entry.day_index,
				entry.slot_index,
				loc.UtcOffset ?? 0));
		}

		return stats;
	}

	internal static uint StableHash(string locationId)
	{
		const uint fnvPrime = 16777619;
		const uint fnvOffset = 2166136261;
		uint hash = fnvOffset;
		foreach (char c in locationId)
		{
			hash ^= (byte)c;
			hash *= fnvPrime;
		}
		return hash;
	}

	private record LocationWithBatchInfo(string Id, int? UtcOffsetMinutes);
}
