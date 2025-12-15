namespace admin.Models;

public record TimezoneOffset(string TimezoneId, string Offset)
{
	public TimezoneOffset(string timezoneId, TimeSpan offset)
		: this(timezoneId, offset.Format())
	{
	}
}

public static class TimeSpanExtensions
{
	extension(TimeSpan offset)
	{
		public string Format()
		{
			var sign = offset < TimeSpan.Zero ? "-" : "+";
			var normalized = offset.Duration();
			return $"{sign}{normalized.Hours:00}:{normalized.Minutes:00}";
		}
	}
}
