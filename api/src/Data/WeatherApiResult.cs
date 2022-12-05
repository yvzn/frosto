using System;
using System.Collections.Generic;

namespace api.Data;

internal class WeatherApiResult
{
	public IList<WeatherApiForecast> forecasts { get; set; } = Array.Empty<WeatherApiForecast>();
}

internal class WeatherApiForecast
{
	public string? date { get; set; }
	public WeatherApiRange? temperature { get; set; }
}

internal class WeatherApiRange
{
	public WeatherApiUnit? minimum { get; set; }
	public WeatherApiUnit? maximum { get; set; }
}

internal class WeatherApiUnit
{
	public decimal? value { get; set; }
}
