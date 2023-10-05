using System;
using System.Collections.Generic;

namespace batch.Models;

internal class Notification
{
	public string? subject { get; set; }
	public string? body { get; set; }
	public string? raw { get; set; }
	public ICollection<string> to { get; set; } = Array.Empty<string>();
}

