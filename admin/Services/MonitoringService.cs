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

		await foreach (var entity in _monitoringTableClient.QueryAsync<MonitoringEntity>(
			e => e.Timestamp < cutoffDate,
			select: ["PartitionKey", "RowKey"],
			cancellationToken: cancellationToken))
		{
			await _monitoringTableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, cancellationToken: cancellationToken);
			deleted++;
		}

		return deleted;
	}
}
