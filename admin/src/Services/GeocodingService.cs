using admin.Models;
using Azure;
using Azure.Core.GeoJson;
using Azure.Maps.Search;
using Azure.Maps.Search.Models;
using Azure.Maps.TimeZones;

namespace admin.Services;

public class GeocodingService(IConfiguration configuration)
{
	private readonly string _azureMapsSubscriptionKey = configuration.GetConnectionString("AzureMaps") ?? string.Empty;

	public IEnumerable<Geocoding> GetGeocoding(Location location)
	{
		var credential = new AzureKeyCredential(_azureMapsSubscriptionKey);
		var client = new MapsSearchClient(credential);

		var searchResult = client.GetGeocoding($"{location.zipCode?.Trim()} {location.city.Trim()}, {location.country.Trim()}");

		for (int i = 0; i < searchResult.Value.Features.Count; i++)
		{
			yield return BuildGeocodingResult(searchResult.Value.Features[i]);
		}
	}

	private static Geocoding BuildGeocodingResult(FeaturesItem featuresItem)
	{
		return new Geocoding
		{
			Locality = featuresItem.Properties?.Address?.Locality ?? string.Empty,
			Country = featuresItem.Properties?.Address?.CountryRegion?.Name ?? string.Empty,
			District = featuresItem.Properties?.Address?.AdminDistricts?[0]?.ShortName ?? string.Empty,
			Latitude = featuresItem.Geometry?.Coordinates[1] ?? 0,
			Longitude = featuresItem.Geometry?.Coordinates[0] ?? 0
		};
	}

	public TimezoneOffset? GetTimezoneInfo(double latitude, double longitude, CancellationToken cancellationToken)
	{
		var credential = new AzureKeyCredential(_azureMapsSubscriptionKey);
		var client = new MapsTimeZoneClient(credential);

		var options = new GetTimeZoneOptions();
		var coordinates = new GeoPosition(longitude, latitude);

		var timezoneResult = client.GetTimeZoneByCoordinates(coordinates, options, cancellationToken);

		foreach (var timezone in timezoneResult.Value.TimeZones)
		{
			try
			{
				var systemTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timezone.Id);
				var offset = systemTimeZone.GetUtcOffset(DateTimeOffset.UtcNow);
				return new TimezoneOffset(timezone.Id, offset);
			}
			catch (Exception)
			{
				continue;
			}
		}

		return null;
	}
}
