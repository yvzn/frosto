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

	public async Task<Feedback?> GetFeedbackByIdAsync(string id, CancellationToken cancellationToken)
	{
		var (partitionKey, rowKey) = id.ToKeys();
		var response = await _feedbackTableClient.GetEntityIfExistsAsync<FeedbackEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		var entity = response.Value;
		if (entity == null)
		{
			return null;
		}

		return new Feedback
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
		};
	}

	public async Task DeleteAsync(string id, CancellationToken cancellationToken)
	{
		var (partitionKey, rowKey) = id.ToKeys();
		await _feedbackTableClient.DeleteEntityAsync(partitionKey, rowKey, cancellationToken: cancellationToken);
	}

	public async Task RestoreAsync(Feedback feedback, CancellationToken cancellationToken)
	{
		var (partitionKey, rowKey) = feedback.Id.ToKeys();
		if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
		{
			return;
		}

		var entity = new FeedbackEntity
		{
			PartitionKey = partitionKey,
			RowKey = rowKey,
			sentiment = feedback.Sentiment,
			text = feedback.Text,
			email = feedback.Email,
			consent = feedback.Consent,
			lang = feedback.Language,
			source = feedback.Source,
			locationPartitionKey = feedback.LocationPartitionKey,
			locationRowKey = feedback.LocationRowKey,
		};
		await _feedbackTableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
	}
}
