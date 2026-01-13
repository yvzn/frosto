using Azure;

namespace support.Model;

public class UnsubscribeEntity: Azure.Data.Tables.ITableEntity
{
	public string? token { get; set; } = null;

	public required string PartitionKey {get; set; }
	public required string RowKey {get; set; }
	public DateTimeOffset? Timestamp {get; set; }
	public ETag ETag {get; set; }
}
