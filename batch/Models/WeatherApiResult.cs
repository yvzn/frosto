using System;
using System.Collections.Generic;

namespace batch.Models;

internal class WeatherApiResult
{
	public WeatherApiHourlyForecast hourly { get; set; } = new();
	public WeatherApiDailyForecast daily { get; set; } = new();
}

internal class WeatherApiDailyForecast
{
	public IList<DateOnly> time { get; set; } = Array.Empty<DateOnly>();
	public IList<decimal> temperature_2m_max { get; set; } = Array.Empty<decimal>();
	public IList<decimal> temperature_2m_min { get; set; } = Array.Empty<decimal>();
}

internal class WeatherApiHourlyForecast
{
	public IList<DateTime> time { get; set; } = Array.Empty<DateTime>();
	public IList<decimal> soil_temperature_6cm { get; set; } = Array.Empty<decimal>();
}
