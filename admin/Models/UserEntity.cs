using Azure;
using Azure.Data.Tables;

namespace admin.Models;

public class UserEntity : ITableEntity
{
    public string? email { get; set; }

    public string? PartitionKey { get; set; }
    public string? RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
