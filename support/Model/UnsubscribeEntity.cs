using Azure;

namespace support.Model;

public class UnsubscribeEntity: Azure.Data.Tables.ITableEntity
{
	public string? token { get; set; }
	public string? user { get; set; }
	public string? email { get; set; }
	public Guid? id { get; set; }
	public string? reason { get; set; }
	public string? lang { get; set; }

	public required string PartitionKey {get; set; }
	public required string RowKey {get; set; }
	public DateTimeOffset? Timestamp {get; set; }
	public ETag ETag {get; set; }
}
