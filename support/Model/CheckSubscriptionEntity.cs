using Azure;

namespace support.Model;

public class CheckSubscriptionEntity: Azure.Data.Tables.ITableEntity
{
	public string? email { get; set; } = null;

	public required string PartitionKey {get; set; }
	public required string RowKey {get; set; }
	public DateTimeOffset? Timestamp {get; set; }
	public ETag ETag {get; set; }
}
