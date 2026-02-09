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

public class SingleRecipientNotification
{
	public string? rowKey { get; private set; }
	public string? subject { get; private set; }
	public string? body { get; private set; }
	public string? raw { get; private set; }
	public string? lang { get; private set; }
	public NotificationFrom? from { get; private set; }
	public string to { get; private set; }

	public SingleRecipientNotification(string recipient, Notification notification)
	{
		rowKey = notification.rowKey;
		subject = notification.subject;
		body = notification.body;
		raw = notification.raw;
		lang = notification.lang;
		from = notification.from;
		to = recipient;
	}
}
