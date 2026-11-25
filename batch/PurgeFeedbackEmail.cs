using Azure.Data.Tables;
using Frosto.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using System;
using System.Threading.Tasks;

namespace batch;

public class PurgeFeedbackEmail(IAzureClientFactory<TableClient> azureClientFactory)
{
	private readonly TableClient feedbackEntities = azureClientFactory.CreateClient("feedbackTableClient");

	[Function("PurgeFeedbackEmail")]
	public async Task RunAsync(
		[TimerTrigger("0 3 * * *")] TimerInfo _)
	{
		var cutoff = DateTimeOffset.UtcNow.AddDays(-30);

		await foreach (var entity in feedbackEntities.QueryAsync<FeedbackEntity>(feedback => feedback.email != null))
		{
			if (entity.Timestamp is null || entity.Timestamp >= cutoff)
			{
				continue;
			}
			entity.email = null;
			await feedbackEntities.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);
		}
	}
}
