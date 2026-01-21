using admin.Models;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;

namespace admin.Services;

public class CheckSubscriptionService(IAzureClientFactory<TableClient> azureClientFactory)
{
	private readonly TableClient _checkSubscriptionTableClient = azureClientFactory.CreateClient("checksubscriptionTableClient");

	public async Task<IList<CheckSubscription>> GetCheckSubscriptionRequestsAsync(CancellationToken cancellationToken)
	{
		var result = new List<CheckSubscription>();

		await foreach (var entity in _checkSubscriptionTableClient.QueryAsync<CheckSubscriptionEntity>(_ => true, cancellationToken: cancellationToken))
		{
			result.Add(EntityToModel(entity));
		}

		return result;
	}

	public async Task<bool> DeleteCheckSubscriptionAsync(string? id, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(id);

		var (partitionKey, rowKey) = id.ToKeys();

		var response = await _checkSubscriptionTableClient.DeleteEntityAsync(partitionKey, rowKey, cancellationToken: cancellationToken);

		return !response.IsError;
	}

	private static CheckSubscription EntityToModel(CheckSubscriptionEntity entity)
	{
		return new()
		{
			email = entity.email ?? "",
			userConsent = entity.userConsent ?? "",
			lang = entity.lang ?? "",
			PartitionKey = entity.PartitionKey ?? "",
			RowKey = entity.RowKey ?? "",
			Timestamp = entity.Timestamp,
		};
	}
}
