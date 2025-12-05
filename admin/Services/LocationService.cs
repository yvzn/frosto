using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using admin.Models;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;

namespace admin.Services;

public partial class LocationService(IAzureClientFactory<TableClient> azureClientFactory)
{
	private readonly TableClient _validLocationTableClient = azureClientFactory.CreateClient("validlocationTableClient");
	private readonly TableClient _locationTableClient = azureClientFactory.CreateClient("locationTableClient");

	public async Task<Location?> GetLocationAsync(string id, CancellationToken cancellationToken)
	{
		var (partitionKey, rowKey) = id.ToKeys();

		var locationEntity = await _locationTableClient.GetEntityAsync<LocationEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		if (locationEntity.HasValue)
		{
			return EntityToModel(locationEntity.Value);
		}
		return default;
	}

	internal async Task<bool> CreateValidLocationAsync(Location? validLocation, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(validLocation);

		var (partitionKey, rowKey) = validLocation.Id.ToKeys();

		var entity = await _locationTableClient.GetEntityAsync<LocationEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		var validLocationEntity = entity.Value;

		validLocationEntity.city = validLocation.city.Trim();
		validLocationEntity.country = Capitalize(validLocation.country);
		validLocationEntity.coordinates = validLocation.coordinates.Replace(" ", "");
		validLocationEntity.users = validLocation.users.Trim();
		validLocationEntity.uat = null;
		validLocationEntity.channel = string.IsNullOrWhiteSpace(validLocation.channel) ? default : validLocation.channel.Trim().ToLower();
		validLocationEntity.zipCode = validLocation.zipCode?.Trim();
		validLocationEntity.lang = validLocation.lang?.Trim().ToLower();
		validLocationEntity.timezone = validLocation.timezone?.Trim();
		validLocationEntity.offset = Normalize(validLocation.offset);
		validLocationEntity.PartitionKey = Capitalize(validLocation.country);
		validLocationEntity.RowKey = validLocation.RowKey;

		await _validLocationTableClient.AddEntityAsync(validLocationEntity, cancellationToken: cancellationToken);

		return true;
	}

	internal async Task<bool> UpdateValidLocationAsync(Location? validLocation, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(validLocation);

		var (partitionKey, rowKey) = validLocation.Id.ToKeys();

		var entity = await _validLocationTableClient.GetEntityAsync<LocationEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		var validLocationEntity = entity.Value;

		validLocationEntity.city = validLocation.city.Trim();
		validLocationEntity.country = Capitalize(validLocation.country);
		validLocationEntity.coordinates = validLocation.coordinates.Replace(" ", "");
		validLocationEntity.users = validLocation.users.Trim();
		validLocationEntity.uat = validLocation.uat ? true : validLocationEntity.uat.HasValue ? false : null;
		validLocationEntity.channel = string.IsNullOrWhiteSpace(validLocation.channel) ? default : validLocation.channel.Trim().ToLower();
		validLocationEntity.zipCode = validLocation.zipCode?.Trim();
		validLocationEntity.lang = validLocation.lang?.Trim().ToLower();
		validLocationEntity.timezone = validLocation.timezone?.Trim();
		validLocationEntity.offset = Normalize(validLocation.offset);
		validLocationEntity.PartitionKey = Capitalize(validLocation.country);
		validLocationEntity.RowKey = validLocation.RowKey;

		await _validLocationTableClient.UpdateEntityAsync(validLocationEntity, validLocationEntity.ETag, cancellationToken: cancellationToken);

		return true;
	}

	internal async Task<IList<Location>> GetValidLocationsAsync(CancellationToken cancellationToken)
	{
		var result = new List<Location>();
		await foreach (var validLocationEntity in _validLocationTableClient.QueryAsync<LocationEntity>(_ => true, cancellationToken: cancellationToken))
		{
			result.Add(EntityToModel(validLocationEntity));
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

	public async Task<Location?> GetValidLocationAsync(string id, CancellationToken cancellationToken)
	{
		var (partitionKey, rowKey) = id.ToKeys();

		var validLocationEntity = await _validLocationTableClient.GetEntityAsync<LocationEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		if (validLocationEntity.HasValue)
		{
			return EntityToModel(validLocationEntity.Value);
		}
		return default;
	}

	internal async Task<Location?> FindValidLocationAsync(string city, string country, CancellationToken cancellationToken)
	{
		var cityQuery = city.Trim();
		var countryQuery = country.Trim();

		await foreach (var validLocationEntity in _validLocationTableClient.QueryAsync<LocationEntity>(e => e.city == cityQuery && e.country == countryQuery, cancellationToken: cancellationToken))
		{
			return EntityToModel(validLocationEntity);
		}
		return default;
	}

	internal async Task<IList<Location>> FindValidLocationsByUserAsync(string email, CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(email);

		var normalizedEmail = email.Trim().ToLowerInvariant();
		var matches = new List<Location>();

		await foreach (var validLocationEntity in _validLocationTableClient.QueryAsync<LocationEntity>(_ => true, cancellationToken: cancellationToken))
		{
			if (string.IsNullOrWhiteSpace(validLocationEntity.users))
			{
				continue;
			}

			var userEntries = validLocationEntity.users
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(entry => entry.ToLowerInvariant());

			if (userEntries.Contains(normalizedEmail))
			{
				matches.Add(EntityToModel(validLocationEntity));
			}
		}

		return matches;
	}

	internal async Task<IList<Location>> FindLocationsByUserAsync(string email, CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(email);

		var normalizedEmail = email.Trim().ToLowerInvariant();
		var matches = new List<Location>();

		await foreach (var locationEntity in _locationTableClient.QueryAsync<LocationEntity>(_ => true, cancellationToken: cancellationToken))
		{
			if (string.Equals(locationEntity.users?.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase))
			{
				matches.Add(EntityToModel(locationEntity));
			}
		}

		return matches;
	}

	private static Location EntityToModel(LocationEntity locationEntity)
	{
		return new()
		{
			city = locationEntity.city ?? "",
			country = locationEntity.country ?? "",
			coordinates = locationEntity.coordinates ?? "",
			users = locationEntity.users ?? "",
			uat = locationEntity.uat ?? default,
			channel = locationEntity.channel ?? "",
			zipCode = locationEntity.zipCode ?? "",
			lang = locationEntity.lang ?? "",
			timezone = locationEntity.timezone ?? "",
			offset = locationEntity.offset ?? "",
			PartitionKey = locationEntity.PartitionKey ?? "",
			RowKey = locationEntity.RowKey ?? "",
			Timestamp = locationEntity.Timestamp,
		};
	}

	private static string Capitalize(string s) => s.Trim()[0].ToString().ToUpper() + s.Trim()[1..].ToLower();

	[GeneratedRegex(@"^([\+\-])([0-9]|[01][0-9]|2[0-3]):?([0-9]|[0-5][0-9])?$")]
	private static partial Regex TimezoneOffsetRegex();

	private static string? Normalize(string? s)
	{
		if (s is null)
		{
			return null;
		}

		var trimmed = s.Trim();

		var matches = TimezoneOffsetRegex().Matches(trimmed);
		if (matches.Count == 0)
		{
			return trimmed;
		}

		var sign = matches[0].Groups[1].Value;
		var hours = matches[0].Groups[2].Value.PadLeft(2, '0');
		var minutes = matches[0].Groups.Count > 3
			? matches[0].Groups[3].Value.PadLeft(2, '0')
			: "00";

		return $"{sign}{hours}:{minutes}";
	}
}

