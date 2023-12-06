using System.Runtime.CompilerServices;
using admin.Models;
using Azure.Data.Tables;

namespace admin.Services;

public class BatchService
{
	private readonly TableClient _batchTableClient;
	private readonly TableClient _validLocationTableClient;

	public BatchService(IConfiguration configuration)
	{
		_batchTableClient = new TableClient(
			configuration.GetConnectionString("Alerts"),
			tableName: "batch");
		_validLocationTableClient = new TableClient(
			configuration.GetConnectionString("Alerts"),
			tableName: "validlocation");
	}

	internal async Task DeleteAllBatches(CancellationToken cancellationToken)
	{
		await foreach (var batchEntity in _batchTableClient.QueryAsync<TableEntity>(select: new[] { "PartitionKey", "RowKey" }, cancellationToken: cancellationToken))
		{
			await _batchTableClient.DeleteEntityAsync(batchEntity.PartitionKey, batchEntity.RowKey, cancellationToken: cancellationToken);
		}
	}

	internal async Task<int> CreateBatches(int periodInDays, int batchCountPerDay, CancellationToken cancellationToken)
	{
		var validLocationIds = GetValidLocationIdsAsync(cancellationToken);

		var batchCount = periodInDays * batchCountPerDay;

		var batches = await validLocationIds.AsBatchesAsync(batchCount, cancellationToken);

		foreach (var batchEntity in batches.ConvertToBatchEntities(batchCountPerDay))
		{
			await _batchTableClient.AddEntityAsync(batchEntity, cancellationToken: cancellationToken);
		}

		return batchCount;
	}

	private async IAsyncEnumerable<string> GetValidLocationIdsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
	{
		await foreach (var validLocationEntity in _validLocationTableClient.QueryAsync<TableEntity>(select: new[] { "PartitionKey", "RowKey" }, cancellationToken: cancellationToken))
		{
			yield return (validLocationEntity.PartitionKey, validLocationEntity.RowKey).ToId();
		}
	}
}

internal static class IAsyncEnumerableExtensions
{
	public static async Task<IList<ICollection<TValue>>> AsBatchesAsync<TValue>(this IAsyncEnumerable<TValue> source, int batchCount, CancellationToken cancellationToken)
	{
		var batches = new ICollection<TValue>[batchCount];
		int itemIndex = 0;

		await foreach (var value in source.WithCancellation(cancellationToken))
		{
			var batchNumber = itemIndex % batchCount;
			if (batches[batchNumber] is null)
			{
				batches[batchNumber] = new HashSet<TValue>();
			}
			batches[batchNumber].Add(value);
			++itemIndex;
		}

		return batches;
	}
}

internal static class IListExtensions
{
	public static IEnumerable<BatchEntity> ConvertToBatchEntities<TValue>(this IList<ICollection<TValue>> batches, int batchCountPerDay)
		=> batches.Select((batch, batchNumber)
			=>
			{
				int day = batchNumber / batchCountPerDay;
				int group = batchNumber % batchCountPerDay;

				return new BatchEntity
				{
					PartitionKey = $"day-{day}",
					RowKey = $"day-{day}-group-{group}",
					locations = string.Join(' ', batch),
				};
			});
}