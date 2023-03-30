using System;
using System.Collections.Generic;

namespace api.Data;

internal class WeatherApiResult
{
	public WeatherApiForecast daily { get; set; } = new();
}

internal class WeatherApiForecast
{
	public IList<DateTime> time { get; set; } = Array.Empty<DateTime>();
	public IList<decimal> temperature_2m_max { get; set; } = Array.Empty<decimal>();
	public IList<decimal> temperature_2m_min { get; set; } = Array.Empty<decimal>();
}
