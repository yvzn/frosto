namespace weather;

public record Forecast(DateOnly Date, decimal Minimum, decimal Maximum)
{
	internal static readonly decimal defaultTemperatureThreshold = 1.0m;
}
