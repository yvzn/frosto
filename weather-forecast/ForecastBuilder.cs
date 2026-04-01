namespace weather;

public class ForecastBuilder(
	OpenMeteoApiResult? weatherApiResult,
	ILocation? location,
	bool applyTemperatureThreshold = false)
{
	public List<Forecast>? Build()
	{
		var forecasts = FromWeatherApiResult();
		if (forecasts.Count is 0)
		{
			return null;
		}

		forecasts = AdjustForLocation(forecasts);

		forecasts = ApplyTemperatureThreshold(forecasts);

		return forecasts;
	}

	private List<Forecast> FromWeatherApiResult()
	{
		var forecasts = new List<Forecast>();

		if (weatherApiResult?.hourly.time.Count is > 0)
		{
			var emptyArray = new decimal?[weatherApiResult.hourly.time.Count];

			forecasts.AddRange(
				weatherApiResult.hourly.time
					.Zip(weatherApiResult.hourly.soil_temperature_0cm ?? emptyArray)
					.Where(tuple => tuple.Second.HasValue)
					.GroupBy(tuple => DateOnly.FromDateTime(tuple.First))
					.Select(group => new Forecast(
						group.Key,
						group.Min(tuple => tuple.Second!.Value),
						group.Max(tuple => tuple.Second!.Value)))
			);

			forecasts.AddRange(
				weatherApiResult.hourly.time
					.Zip(weatherApiResult.hourly.soil_temperature_6cm ?? emptyArray)
					.Where(tuple => tuple.Second.HasValue)
					.GroupBy(tuple => DateOnly.FromDateTime(tuple.First))
					.Select(group => new Forecast(
						group.Key,
						group.Min(tuple => tuple.Second!.Value),
						group.Max(tuple => tuple.Second!.Value)))
			);

			forecasts.AddRange(
				weatherApiResult.hourly.time
					.Zip(weatherApiResult.hourly.apparent_temperature ?? emptyArray)
					.Where(tuple => tuple.Second.HasValue)
					.GroupBy(tuple => DateOnly.FromDateTime(tuple.First))
					.Select(group => new Forecast(
						group.Key,
						group.Min(tuple => tuple.Second!.Value),
						group.Max(tuple => tuple.Second!.Value),
						IsApparentTemperature: true))
			);
		}

		if (weatherApiResult?.daily.time.Count is > 0)
		{
			var emptyArray = new decimal?[weatherApiResult.hourly.time.Count];

			forecasts.AddRange(
				weatherApiResult.daily.time
					.Zip(
						weatherApiResult.daily.temperature_2m_min ?? emptyArray,
						weatherApiResult.daily.temperature_2m_max ?? emptyArray)
					.Where(tuple => tuple.Second.HasValue && tuple.Third.HasValue)
					.Select(tuple => new Forecast(
						tuple.First,
						tuple.Second!.Value,
						tuple.Third!.Value))
			);

			forecasts.AddRange(
				weatherApiResult.daily.time
					.Zip(
						weatherApiResult.daily.apparent_temperature_min ?? emptyArray,
						weatherApiResult.daily.apparent_temperature_max ?? emptyArray)
					.Where(tuple => tuple.Second.HasValue && tuple.Third.HasValue)
					.Select(tuple => new Forecast(
						tuple.First,
						tuple.Second!.Value,
						tuple.Third!.Value,
						IsApparentTemperature: true))
			);
		}

		forecasts = [.. forecasts
			.GroupBy(f => f.Date)
			.Select(group =>
			{
				var min = group.MinBy(f => f.Minimum);
				var max = group.MaxBy(f => f.Maximum);
				var isApparentTemperature = min!.IsApparentTemperature || max!.IsApparentTemperature;
				return new Forecast(group.Key, min!.Minimum, max!.Maximum, isApparentTemperature);
			})];

		return forecasts;
	}

	private List<Forecast> AdjustForLocation(List<Forecast> forecasts)
	{
		if (location?.minTemperatureAdjustment.HasValue is true)
		{
			var adjustment = Convert.ToDecimal(location.minTemperatureAdjustment.Value);
			return [.. forecasts.Select(f => f with { Minimum = f.Minimum + adjustment })];
		}

		return forecasts;
	}

	private List<Forecast> ApplyTemperatureThreshold(List<Forecast> forecasts)
	{
		if (!applyTemperatureThreshold)
		{
			return forecasts;
		}

		var threshold = location?.minThreshold.HasValue is true
			? Convert.ToDecimal(location.minThreshold.Value)
			: Forecast.defaultTemperatureThreshold;

		return [.. forecasts.Where(f => f.Minimum <= threshold)];
	}
}
