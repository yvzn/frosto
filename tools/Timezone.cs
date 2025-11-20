using System.Text.Json.Serialization;

namespace tools;

public class Timezone
{
	[JsonPropertyName("id")] public string? Id { get; set; }
	[JsonPropertyName("type")] public string? Type { get; set; }
	[JsonPropertyName("offset")] public TimezoneOffset? Offset { get; set; }
	[JsonPropertyName("abbr")] public string? Abbr { get; set; }
}

public class TimezoneOffset
{
	[JsonPropertyName("sdt")] public string? Sdt { get; set; }
	[JsonPropertyName("dst")] public string? Dst { get; set; }
}
