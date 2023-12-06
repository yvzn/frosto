using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Telemetry;

namespace backup;

public class BackupTables
{
    private static readonly TableServiceClient backupTableServiceClient = new(Environment.GetEnvironmentVariable("BACKUP_CONNECTION_STRING"));

    [FunctionName("BackupTables")]
    public async Task RunAsync(
    [TimerTrigger("0 0 1 * 1-5,9-12 *"
#if DEBUG
		, RunOnStartup=true
#endif
	)]
        TimerInfo timerInfo,
    ILogger log)
    {
#if DEBUG
        await Task.Delay(5_000);
#endif

        await BackupTablesAsync(log)(new[] { "batch", "location", "signup", "user", "validlocation" });
    }

    private Func<string[], Task> BackupTablesAsync(ILogger log)
        => (tables) => Task.WhenAll(tables.Select(BackupTableAsync(log)));

    private Func<string, Task> BackupTableAsync(ILogger log)
        => (tableName) => BackupTableAsync(tableName, log);

    private static async Task BackupTableAsync(string tableName, ILogger log)
    {
        try
        {
            log.LogInformation("Backup of table {TableName}", tableName);

            var resiliencePipeline = GetResiliencePipeline(log);

            await resiliencePipeline.ExecuteAsync(
                async (CancellationToken ct) => await backupTableServiceClient.DeleteTableIfExistsAsync(tableName, ct));

            await resiliencePipeline.ExecuteAsync(
                async (CancellationToken ct) => await backupTableServiceClient.CreateTableAsync(tableName, ct));

            var entityCount = await BackupEntities(tableName);

            log.LogInformation("Backup of table {TableName} completed with {EntityCount} entries", tableName, entityCount);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Backup of table {TableName} failed", tableName);
        }
    }

    private static async Task<int> BackupEntities(string tableName)
    {
        var entityCount = 0;
        var sourceTableClient = new TableClient(Environment.GetEnvironmentVariable("SOURCE_CONNECTION_STRING"), tableName);
        var backupTableClient = new TableClient(Environment.GetEnvironmentVariable("BACKUP_CONNECTION_STRING"), tableName);

        await foreach (var sourceEntity in sourceTableClient.QueryAsync<TableEntity>(_ => true))
        {
            var backupEntity = new TableEntity
            {
                PartitionKey = sourceEntity.PartitionKey,
                RowKey = sourceEntity.RowKey
            };
            foreach (var property in sourceEntity.Keys)
            {
                backupEntity[property] = sourceEntity[property];
            }
            await backupTableClient.AddEntityAsync(backupEntity);
            ++entityCount;
        }

        return entityCount;
    }

    private static ResiliencePipeline GetResiliencePipeline(ILogger logger)
        => new ResiliencePipelineBuilder()
            .AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Azure.RequestFailedException>(),
                Delay = TimeSpan.FromSeconds(5),
                MaxRetryAttempts = 10,
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = (OnRetryArguments<object> args) =>
                {
                    logger.LogDebug("Retry attempt: {AttemptNumber} delay: {RetryDelay}", args.AttemptNumber, args.RetryDelay);                    
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromMinutes(2))
            .Build();
}

public static class TableServiceClientExtensions
{
    public async static Task DeleteTableIfExistsAsync(this TableServiceClient tableServiceClient, string tableName, CancellationToken cancellationToken)
    {
        var queryTableResults = tableServiceClient.QueryAsync(filter: $"TableName eq '{tableName}'", cancellationToken: cancellationToken);
        await foreach (var table in queryTableResults.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await tableServiceClient.DeleteTableAsync(table.Name, cancellationToken);
        }
    }
}