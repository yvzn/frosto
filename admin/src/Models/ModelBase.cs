using System.ComponentModel.DataAnnotations;

namespace admin.Models;

public abstract class ModelBase
{
	[Required]
	internal string PartitionKey { get; set; } = string.Empty;

	[Required]
	internal string RowKey { get; set; } = string.Empty;

	public DateTimeOffset? Timestamp { get; set; }

	protected string GetId() => (PartitionKey, RowKey).ToId();

	protected void SetId(string? value)
	{
		(var partitionKey, var rowKey) = value.ToKeys();
		PartitionKey = partitionKey ?? string.Empty;
		RowKey = rowKey ?? string.Empty;
	}
}
