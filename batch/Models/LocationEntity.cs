
using System;
using Azure;
using Azure.Data.Tables;

namespace batch.Models;

public class LocationEntity : ITableEntity
{
	public string? city { get; set; }
	public string? country { get; set; }
	public string? coordinates { get; set; }
	public string? timezone { get; set; }
	public string? users { get; set; }
	public bool? uat { get; set; }
	public string? channel { get; set; }
	public double? minThreshold { get; set; }
	public string? weatherApiUrl { get; set; }
	public string? PartitionKey { get; set; }
	public string? RowKey { get; set; }
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }
}
