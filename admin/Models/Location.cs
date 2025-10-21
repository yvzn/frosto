using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace admin.Models;

public class Location
{
	[Required]
	[RegularExpression(@"^[^\d]+$")]
	[DisplayName("City")]
	public string city { get; set; } = "";
	[Required]
	[DisplayName("Country")]
	public string country { get; set; } = "";
	[Required]
	[RegularExpression(@"^-?[\d\.]+,\s*-?[\d\.]+$")]
	[DisplayName("Coordinates")]
	public string coordinates { get; set; } = "";
	[Required]
	[DisplayName("Users")]
	public string users { get; set; } = "";
	[DisplayName("UAT")]
	public bool uat { get; set; } = false;
	[DisplayName("Channel")]
	public string? channel { get; set; } = "";
	[DisplayName("Zip Code")]
	public string? zipCode { get; set; } = "";
	[DisplayName("Language")]
	public string? lang { get; set; } = "";
	[DisplayName("Timezone")]
	[RegularExpression(@"^(Africa|America|Antarctica|Arctic|Asia|Atlantic|Australia|Europe|Indian|Pacific|Etc)(/[A-Za-z0-9_\-\+]+){1,2}$")]
	public string? timezone { get; set; } = "";
	[DisplayName("Offset")]
	[RegularExpression(@"^[\+\-]?([01][0-9]|2[0-3]):?[0-5][0-9]$")]
	public string? offset { get; set; } = "";

	[Required]
	internal string PartitionKey { get; set; } = "";
	[Required]
	internal string RowKey { get; set; } = "";
	public DateTimeOffset? Timestamp { get; set; }

	[HiddenInput]
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

public static class LocationExtensions
{
	public static string ToId(this (string? PartitionKey, string? RowKey) tuple)
	{
		return $"{tuple.PartitionKey ?? ""}|{tuple.RowKey ?? ""}";
	}

	public static (string? PartitionKey, string? RowKey) ToKeys(this string? id)
	{
		var split = id?.Split('|');
		if (split is [var partitionKey, var rowKey, ..])
		{
			return (partitionKey, rowKey);
		}
		return (default, default);
	}
}
