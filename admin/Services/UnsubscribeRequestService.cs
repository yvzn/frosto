using admin.Models;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;

namespace admin.Services;

public class UnsubscribeRequestService(IAzureClientFactory<TableClient> azureClientFactory)
{
	private readonly TableClient _unsubscribeTableClient = azureClientFactory.CreateClient("unsubscribeTableClient");

	public async Task<IList<Unsubscribe>> GetUnsubscribeRequestsAsync(CancellationToken cancellationToken)
	{
		var result = new List<Unsubscribe>();

		await foreach (var entity in _unsubscribeTableClient.QueryAsync<UnsubscribeEntity>(_ => true, cancellationToken: cancellationToken))
		{
			result.Add(EntityToModel(entity));
		}

		return result;
	}

	private static Unsubscribe EntityToModel(UnsubscribeEntity entity)
	{
		return new()
		{
			token = entity.token ?? "",
			lang = entity.lang ?? "",
			PartitionKey = entity.PartitionKey ?? "",
			RowKey = entity.RowKey ?? "",
			Timestamp = entity.Timestamp,
		};
	}
}
