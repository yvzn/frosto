using System.Collections.Generic;

namespace batch.Models;

public class Notification
{
	public required string? rowKey { get; init; }
	public required string? subject { get; init; }
	public required string? body { get; init; }
	public required string? raw { get; init; }
	public required string? lang { get; init; }
	public required NotificationFrom? from { get; init; }
	public ICollection<string> to { get; set; } = [];
}

public class NotificationFrom
{
	public required string? address { get; init; }
	public required string? displayName { get; init; }
}
