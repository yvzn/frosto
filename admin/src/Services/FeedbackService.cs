using admin.Models;
using Azure.Data.Tables;
using Frosto.Entities;
using Microsoft.Extensions.Azure;

namespace admin.Services;

public class FeedbackService(IAzureClientFactory<TableClient> azureClientFactory)
{
	private readonly TableClient _feedbackTableClient = azureClientFactory.CreateClient("feedbackTableClient");

	public async Task<IList<Feedback>> GetFeedbackAsync(CancellationToken cancellationToken)
	{
		var result = new List<Feedback>();
		await foreach (var entity in _feedbackTableClient.QueryAsync<FeedbackEntity>(_ => true, cancellationToken: cancellationToken))
		{
			result.Add(new Feedback
			{
				Sentiment = entity.sentiment ?? "",
				Text = entity.text ?? "",
				Email = entity.email ?? "",
				Consent = entity.consent,
				Language = entity.lang ?? "",
				Source = entity.source ?? "",
				LocationPartitionKey = entity.locationPartitionKey ?? "",
				LocationRowKey = entity.locationRowKey ?? "",
				PartitionKey = entity.PartitionKey,
				RowKey = entity.RowKey,
				Timestamp = entity.Timestamp,
			});
		}
		return result;
	}

	public async Task DeleteAsync(string id, CancellationToken cancellationToken)
	{
		var (partitionKey, rowKey) = id.ToKeys();
		await _feedbackTableClient.DeleteEntityAsync(partitionKey, rowKey, cancellationToken: cancellationToken);
	}
}
