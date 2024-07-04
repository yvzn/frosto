using admin.Models;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;

namespace admin.Services;

public class LocationService(IAzureClientFactory<TableClient> azureClientFactory)
{
	private readonly TableClient _validLocationTableClient = azureClientFactory.CreateClient("validlocationTableClient");
	private readonly TableClient _locationTableClient = azureClientFactory.CreateClient("locationTableClient");

	public async Task<Location?> GetLocationAsync(string id, CancellationToken cancellationToken)
	{
		var (partitionKey, rowKey) = id.ToKeys();

		var locationEntity = await _locationTableClient.GetEntityAsync<LocationEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		if (locationEntity.HasValue)
		{
			return new()
			{
				city = locationEntity.Value.city ?? "",
				country = locationEntity.Value.country ?? "",
				coordinates = locationEntity.Value.coordinates ?? "",
				users = locationEntity.Value.users ?? "",
				channel = locationEntity.Value.channel ?? "",
				zipCode = locationEntity.Value.zipCode ?? "",
				lang = locationEntity.Value.lang ?? "",
				PartitionKey = locationEntity.Value.PartitionKey ?? "",
				RowKey = locationEntity.Value.RowKey ?? "",
				Timestamp = locationEntity.Value.Timestamp,
			};
		}
		return default;
	}

	internal async Task<bool> PutValidLocationAsync(Location? validLocation, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(validLocation);

		var (partitionKey, rowKey) = validLocation.Id.ToKeys();

		var entity = await _locationTableClient.GetEntityAsync<LocationEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		var validLocationEntity = entity.Value;


		validLocationEntity.city = validLocation.city.Trim();
		validLocationEntity.country = Capitalize(validLocation.country);
		validLocationEntity.coordinates = validLocation.coordinates.Replace(" ", "");
		validLocationEntity.users = validLocation.users.Trim();
		validLocationEntity.channel = string.IsNullOrWhiteSpace(validLocation.channel) ? default : validLocation.channel.Trim().ToLower();
		validLocationEntity.zipCode = validLocation.zipCode.Trim();
		validLocationEntity.lang = validLocation.lang.Trim().ToLower();
		validLocationEntity.PartitionKey = Capitalize(validLocation.country);
		validLocationEntity.RowKey = validLocation.RowKey;

		await _validLocationTableClient.AddEntityAsync(validLocationEntity, cancellationToken: cancellationToken);

		return true;
	}

	internal async Task<IList<Location>> GetValidLocationsAsync(CancellationToken cancellationToken)
	{
		var result = new List<Location>();
		await foreach (var validLocationEntity in _validLocationTableClient.QueryAsync<LocationEntity>(_ => true, cancellationToken: cancellationToken))
		{
			result.Add(new()
			{
				city = validLocationEntity.city ?? "",
				country = validLocationEntity.country ?? "",
				coordinates = validLocationEntity.coordinates ?? "",
				users = validLocationEntity.users ?? "",
				channel = validLocationEntity.channel ?? "",
				zipCode = validLocationEntity.zipCode ?? "",
				lang = validLocationEntity.lang ?? "",
				PartitionKey = validLocationEntity.PartitionKey ?? "",
				RowKey = validLocationEntity.RowKey ?? "",
				Timestamp = validLocationEntity.Timestamp,
			});
		}

		return result;
	}

	internal async Task<object> GetValidLocationsGeoJSONAsync(CancellationToken cancellationToken)
	{
		var features = new List<object>();
		await foreach (var validLocationEntity in _validLocationTableClient.QueryAsync<LocationEntity>(_ => true, cancellationToken: cancellationToken))
		{
			var city = validLocationEntity.city;
			var coordinates = validLocationEntity.coordinates?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			if (coordinates is [var latitude, var longitude, ..])
			{
				features.Add(new
				{
					type = "Feature",
					geometry = new
					{
						type = "Point",
						coordinates = new[] { longitude, latitude },
					},
					properties = new
					{
						city,
					}
				});
			}
		}

		return new
		{
			type = "FeatureCollection",
			features
		};
	}

	internal async Task<Location?> FindLocationAsync(string city, string country, CancellationToken cancellationToken)
	{
		var cityQuery = city.Trim();
		var countryQuery = country.Trim();

		await foreach (var validLocationEntity in _validLocationTableClient.QueryAsync<LocationEntity>(e => e.city == cityQuery && e.country == countryQuery, cancellationToken: cancellationToken))
		{
			return new Location()
			{
				city = validLocationEntity.city ?? "",
				country = validLocationEntity.country ?? "",
				coordinates = validLocationEntity.coordinates ?? "",
				users = validLocationEntity.users ?? "",
				channel = validLocationEntity.channel ?? "",
				zipCode = validLocationEntity.zipCode ?? "",
				lang = validLocationEntity.lang ?? "",
				PartitionKey = validLocationEntity.PartitionKey ?? "",
				RowKey = validLocationEntity.RowKey ?? "",
				Timestamp = validLocationEntity.Timestamp,
			};
		}

		return default;
	}

	private static string Capitalize(string s) => s.Trim()[0].ToString().ToUpper() + s.Trim()[1..].ToLower();
}

