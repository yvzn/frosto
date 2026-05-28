using System;
using Azure;
using Azure.Data.Tables;

namespace batch.Models;

public class MonitoringEntity : ITableEntity
{
	public string? coordinates { get; set; }
	public int? daysUntilNextFrost { get; set; }
	public string? PartitionKey { get; set; }
	public string? RowKey { get; set; }
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }
}
