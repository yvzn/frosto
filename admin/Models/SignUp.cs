namespace admin.Models;

public class SignUp
{
	public string city { get; set; } = "";
	public string country { get; set; } = "";

	internal string PartitionKey { get; set; } = "";
	internal string RowKey { get; set; } = "";
	public DateTimeOffset? Timestamp { get; set; }

	public string Id
	{
		get
		{
			return (PartitionKey, RowKey).ToId();
		}
		set
		{
			(var partitionKey, var rowKey) = value.ToKeys();
			PartitionKey = partitionKey ?? "";
			RowKey = rowKey ?? "";
		}
	}
}
