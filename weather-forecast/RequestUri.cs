using System.Globalization;

namespace weather;

public static class RequestUri
{
    public static Uri From(ILocation location, string defaultWeatherApiUrl)
    {
		ArgumentNullException.ThrowIfNull(location.coordinates, nameof(location));

        var (latitude, longitude) = ParseCoordinates(location.coordinates);
        if (!latitude.HasValue || !longitude.HasValue) throw new ArgumentOutOfRangeException(nameof(location), location.coordinates, "Expected comma-separated numbers");

        var timeZone = location.timezone ?? "Europe/Berlin";

        var weatherApiUrl = location.weatherApiUrl ?? defaultWeatherApiUrl;

        var requestUri = string.Format(CultureInfo.InvariantCulture, weatherApiUrl, latitude.Value, longitude.Value, timeZone);
        return new Uri(requestUri);
    }

    private static (decimal? latitude, decimal? longitude) ParseCoordinates(string coordinates)
    {
        decimal? latitude = default;
        decimal? longitude = default;

        var split = coordinates.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (split.Length > 0 && decimal.TryParse(split[0], CultureInfo.InvariantCulture, out var parsedAt0))
        {
            latitude = parsedAt0;
        }
        if (split.Length > 1 && decimal.TryParse(split[1], CultureInfo.InvariantCulture, out var parsedAt1))
        {
            longitude = parsedAt1;
        }

        return (latitude, longitude);
    }
}
