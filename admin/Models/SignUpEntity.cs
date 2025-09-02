using Azure;
using Azure.Data.Tables;

namespace admin.Models;

public class SignUpEntity : ITableEntity
{
	public string? city { get; set; }
	public string? country { get; set; }
	public string? lang { get; set; }

	public string? PartitionKey { get; set; }
	public string? RowKey { get; set; }
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }
}
