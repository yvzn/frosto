using admin.Models;
using Azure.Data.Tables;

namespace admin.Services;

public class LocationService
{
	private readonly TableClient _validLocationTableClient;
	private readonly TableClient _locationTableClient;
	public DateTimeOffset? LastUpdate { get; private set; } = default;

	public LocationService(IConfiguration configuration)
	{
		_validLocationTableClient = new TableClient(
			configuration.GetConnectionString("Alerts"),
			tableName: "validlocation");
		_locationTableClient = new TableClient(
			configuration.GetConnectionString("Alerts"),
			tableName: "location");
	}

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
				PartitionKey = locationEntity.Value.PartitionKey ?? "",
				RowKey = locationEntity.Value.RowKey ?? "",
				Timestamp = locationEntity.Value.Timestamp,
			};
		}
		return default;
	}

	public async Task<IList<Location>> GetNewLocationsAsync(CancellationToken cancellationToken)
	{
		DateTimeOffset today = DateTimeOffset.UtcNow.Date;

		var validLocationKeys = new HashSet<string>();
		await foreach (var validLocationEntity in _validLocationTableClient.QueryAsync<LocationEntity>(_ => true, cancellationToken: cancellationToken))
		{
			validLocationKeys.Add((validLocationEntity.PartitionKey?.Trim(), validLocationEntity.RowKey?.Trim()).ToId().ToLower());
			if (validLocationEntity.Timestamp < today && (!LastUpdate.HasValue || validLocationEntity.Timestamp > LastUpdate.Value))
			{
				LastUpdate = validLocationEntity.Timestamp;
			}
		}

		var result = new List<Location>();

		await foreach (var locationEntity in _locationTableClient.QueryAsync<LocationEntity>(_ => true, cancellationToken: cancellationToken))
		{
			if (validLocationKeys.Contains((locationEntity.PartitionKey?.Trim(), locationEntity.RowKey?.Trim()).ToId().ToLower()))
			{
				continue;
			}

			result.Add(new()
			{
				city = locationEntity.city ?? "",
				country = locationEntity.country ?? "",
				coordinates = locationEntity.coordinates ?? "",
				users = locationEntity.users ?? "",
				channel = locationEntity.channel ?? "",
				PartitionKey = locationEntity.PartitionKey ?? "",
				RowKey = locationEntity.RowKey ?? "",
				Timestamp = locationEntity.Timestamp,
			});
		}

		return result;
	}

	internal async Task<bool> PutValidLocationAsync(Location? validLocation, CancellationToken cancellationToken)
	{
		if (validLocation is null)
		{
			throw new ArgumentNullException(nameof(validLocation));
		}

		var (partitionKey, rowKey) = validLocation.Id.ToKeys();

		var entity = await _locationTableClient.GetEntityAsync<LocationEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		var validLocationEntity = entity.Value;

		validLocationEntity.city = validLocation.city.Trim();
		validLocationEntity.country = validLocation.country.Trim()[0].ToString().ToUpper() + validLocation.country.Trim()[1..].ToLower();
		validLocationEntity.coordinates = validLocation.coordinates.Replace(" ", "");
		validLocationEntity.users = validLocation.users.Trim();
		validLocationEntity.channel = string.IsNullOrWhiteSpace(validLocation.channel) ? default : validLocation.channel.Trim().ToLower();
		validLocationEntity.PartitionKey = validLocationEntity.country;
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
}

