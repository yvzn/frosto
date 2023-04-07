namespace admin.Models;

public class Location
{
	public string? city { get; set; }
	public string? country { get; set; }
	public string? coordinates { get; set; }

	internal string? PartitionKey { get; set; }
	internal string? RowKey { get; set; }

	public string Id
	{
		get
		{
			return $"{PartitionKey ?? ""}|{RowKey ?? ""}";
		}
		set
		{
			var split = value.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			if (split is [var partitionKey, var rowKey, ..])
			{
				PartitionKey = partitionKey;
				RowKey = rowKey;
			}
			else
			{
				PartitionKey = RowKey = default;
			}
		}
	}
}
