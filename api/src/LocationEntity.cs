
using System;
using Azure;
using Azure.Data.Tables;

namespace api;

public class LocationEntity : ITableEntity
{
	public string? city { get; set; }
	public string? country { get; set; }
	public string? coordinates { get; set; }
	public string? users { get; set; }

	public string? PartitionKey { get; set; }
	public string? RowKey { get; set; }
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }
}