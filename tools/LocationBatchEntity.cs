using Azure;
using Azure.Data.Tables;

namespace tools;

public class LocationBatchEntity : ITableEntity
{
	public int slot_index { get; set; }
	public int day_index { get; set; }
	public string? PartitionKey { get; set; }
	public string? RowKey { get; set; }
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }
}
