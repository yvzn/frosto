using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using batch.Models;
using batch.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace batch;

public class RecordMonitoring(IAzureClientFactory<TableClient> azureClientFactory, ILogger<RecordMonitoring> logger)
{
	private readonly TableClient monitoringTableClient = azureClientFactory.CreateClient("monitoringTableClient");

	[Function("RecordMonitoring")]
	public async Task<IActionResult> RunAsync(
		[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
		HttpRequest req)
	{
		var data = default(MonitoringData);
		try
		{
			data = await System.Text.Json.JsonSerializer.DeserializeAsync<MonitoringData>(req.Body);
			if (data is null || string.IsNullOrWhiteSpace(data.coordinates))
			{
				logger.LogWarning("Skip recording monitoring data: invalid");
				return new BadRequestResult();
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to decode monitoring data");
			return new BadRequestResult();
		}

		try
		{
			var today = DateTimeOffset.UtcNow;
			var entity = new MonitoringEntity
			{
				PartitionKey = today.ToString("yyyy-MM-dd"),
				RowKey = Guid.NewGuid().ToString(),
				coordinates = data.coordinates,
				daysUntilNextFrost = data.daysUntilNextFrost,
			};

			async ValueTask upsert(CancellationToken cancellationToken) => await monitoringTableClient.AddEntityAsync(entity, cancellationToken: cancellationToken);
			await RetryStrategy.For.DataAccess.ExecuteAsync(upsert);

			return new OkResult();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to record monitoring data for {Coordinates}", data.coordinates);
			return new StatusCodeResult(StatusCodes.Status502BadGateway);
		}
	}
}

public class MonitoringData
{
	public string? coordinates { get; set; }
	public int daysUntilNextFrost { get; set; }
}
