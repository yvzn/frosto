using admin.Models;
using Azure;
using Azure.Maps.Search;
using Azure.Maps.Search.Models;

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
			Latitude = featuresItem.Geometry?.Coordinates[1] ?? 0,
			Longitude = featuresItem.Geometry?.Coordinates[0] ?? 0
		};
	}
}
