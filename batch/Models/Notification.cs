using System.Collections.Generic;

namespace batch.Models;

internal class Notification
{
	public required string? subject { get; init; }
	public required string? body { get; init; }
	public required string? raw { get; init; }
	public required NotificationFrom? from { get; init; }
	public ICollection<string> to { get; set; } = [];
}

internal class NotificationFrom
{
	public required string? address { get; init; }
	public required string? displayName { get; init; }
}
