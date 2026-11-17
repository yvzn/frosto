using admin.Models;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;

namespace admin.Services;

public class MonitoringService(IAzureClientFactory<TableClient> azureClientFactory)
{
	private readonly TableClient _monitoringTableClient = azureClientFactory.CreateClient("monitoringTableClient");

	internal async Task<object> GetRecentMonitoringGeoJSONAsync(int lastDays, CancellationToken cancellationToken)
	{
		var cutoffDate = DateTimeOffset.UtcNow.AddDays(-lastDays);
		var features = new List<object>();

		await foreach (var entity in _monitoringTableClient.QueryAsync<MonitoringEntity>(
			e => e.Timestamp >= cutoffDate,
			cancellationToken: cancellationToken))
		{
			var coordinates = entity.coordinates?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			if (coordinates is [var latitude, var longitude, ..])
			{
				features.Add(new
				{
					type = "Feature",
					geometry = new
					{
						type = "Point",
						// GeoJSON uses [longitude, latitude] order (opposite of stored "latitude,longitude")
						coordinates = new[] { longitude, latitude },
					},
					properties = new
					{
						entity.daysUntilNextFrost,
						date = entity.Timestamp?.ToString("yyyy-MM-dd"),
					}
				});
			}
		}

		return new
		{
			type = "FeatureCollection",
			features
		};
	}

	internal async Task<int> DeleteOldMonitoringRecordsAsync(int olderThanDays, CancellationToken cancellationToken)
	{
		var cutoffDate = DateTimeOffset.UtcNow.AddDays(-olderThanDays);
		var deleted = 0;

		// Collect entities to delete, grouped by PartitionKey for batch operations
		var byPartition = new Dictionary<string, List<MonitoringEntity>>();

		await foreach (var entity in _monitoringTableClient.QueryAsync<MonitoringEntity>(
			e => e.Timestamp < cutoffDate,
			select: ["PartitionKey", "RowKey"],
			cancellationToken: cancellationToken))
		{
			var pk = entity.PartitionKey ?? string.Empty;
			if (!byPartition.TryGetValue(pk, out var list))
			{
				list = [];
				byPartition[pk] = list;
			}
			list.Add(entity);
		}

		foreach (var (_, entities) in byPartition)
		{
			// Azure Table Storage batch limit is 100 operations per transaction
			foreach (var batch in entities.Chunk(100))
			{
				var actions = batch.Select(e => new TableTransactionAction(TableTransactionActionType.Delete, e)).ToList();
				await _monitoringTableClient.SubmitTransactionAsync(actions, cancellationToken);
				deleted += batch.Length;
			}
		}

		return deleted;
	}
}
