using System;
using System.Collections.Generic;

namespace api.Data;

internal class Notification
{
	public string? subject { get; set; }
	public string? body { get; set; }
	public ICollection<string> to { get; set; } = Array.Empty<string>();
}

