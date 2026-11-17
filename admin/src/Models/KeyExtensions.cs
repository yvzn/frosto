namespace admin.Models;

public static class KeyExtensions
{
	extension((string? PartitionKey, string? RowKey) tuple)
	{
		public string ToId()
		{
			return $"{tuple.PartitionKey ?? string.Empty}|{tuple.RowKey ?? string.Empty}";
		}
	}

	extension(string? id)
	{
		public (string? PartitionKey, string? RowKey) ToKeys()
		{
			var split = id?.Split('|');
			if (split is [var partitionKey, var rowKey, ..])
			{
				return (partitionKey, rowKey);
			}

			return (default, default);
		}
	}
}
