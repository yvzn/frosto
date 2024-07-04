using System.Runtime.CompilerServices;
using admin.Models;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;

namespace admin.Services;

public class BatchService(IAzureClientFactory<TableClient> azureClientFactory)
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
		await foreach (var validLocationEntity in _validLocationTableClient.QueryAsync<TableEntity>(select: ["PartitionKey", "RowKey"], cancellationToken: cancellationToken))
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
				batches[batchNumber] = [];
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
				int dayNumber = batchNumber / batchCountPerDay;
				int groupNumber = batchNumber % batchCountPerDay;

				return new BatchEntity
				{
					PartitionKey = $"day-{dayNumber}",
					RowKey = $"day-{dayNumber}-group-{groupNumber}",
					locations = string.Join(' ', batch),
				};
			});
}