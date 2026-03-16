namespace weather;

#pragma warning disable IDE1006 // Naming Styles

public class OpenMeteoApiResult
{
	public OpenMeteoApiHourlyForecast hourly { get; set; } = new();
	public OpenMeteoApiDailyForecast daily { get; set; } = new();
}

public class OpenMeteoApiDailyForecast
{
	public IList<DateOnly> time { get; set; } = [];
	public IList<decimal?> temperature_2m_max { get; set; } = [];
	public IList<decimal?> temperature_2m_min { get; set; } = [];
}

public class OpenMeteoApiHourlyForecast
{
	public IList<DateTime> time { get; set; } = [];
	public IList<decimal?> soil_temperature_0cm { get; set; } = [];
	public IList<decimal?> soil_temperature_6cm { get; set; } = [];
}

#pragma warning restore IDE1006 // Naming Styles
