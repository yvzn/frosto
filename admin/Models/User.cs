using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace admin.Models;

public class User
{
    [Required]
    [EmailAddress]
    public string email { get; set; } = string.Empty;

    public string? disabled { get; set; }
    public string? created { get; set; }

    [Required]
    internal string PartitionKey { get; set; } = string.Empty;

    [Required]
    internal string RowKey { get; set; } = string.Empty;

    public DateTimeOffset? Timestamp { get; set; }

    [HiddenInput]
    public string Id
    {
        get => (PartitionKey, RowKey).ToId();
        set
        {
            (var partitionKey, var rowKey) = value.ToKeys();
            PartitionKey = partitionKey ?? string.Empty;
            RowKey = rowKey ?? string.Empty;
        }
    }
}
