using System;
using Azure;
using Azure.Data.Tables;

namespace batch.Models;

public class BatchEntity : ITableEntity
{
	public string? locations { get; set; }

	public string? PartitionKey { get; set; }
	public string? RowKey { get; set; }
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }
}
