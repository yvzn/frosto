using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using admin.Models;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;

namespace admin.Services;

public partial class LocationService(IAzureClientFactory<TableClient> azureClientFactory)
{
	private readonly TableClient _validLocationTableClient = azureClientFactory.CreateClient("validlocationTableClient");
	private readonly TableClient _locationTableClient = azureClientFactory.CreateClient("locationTableClient");
	private readonly TableClient _userTableClient = azureClientFactory.CreateClient("userTableClient");

	public async Task<ValidLocationMoveSource> GetValidLocationMoveSourceAsync(string id, CancellationToken cancellationToken)
	{
		var (partitionKey, rowKey) = id.ToKeys();
		if (string.IsNullOrWhiteSpace(partitionKey) || string.IsNullOrWhiteSpace(rowKey))
		{
			throw new ArgumentException("The location ID is invalid.", nameof(id));
		}

		var response = await _validLocationTableClient.GetEntityAsync<TableEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		var entity = response.Value;
		return new(entity.PartitionKey, entity.RowKey, entity.ETag, ParseUsers(GetProperty<string>(entity, "users")));
	}

	public async Task<string> MoveValidLocationUsersAsync(
		string id,
		string expectedETag,
		IEnumerable<string> selectedEmails,
		CancellationToken cancellationToken)
	{
		var (partitionKey, rowKey) = id.ToKeys();
		ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);
		ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);
		ArgumentException.ThrowIfNullOrWhiteSpace(expectedETag);
		ArgumentNullException.ThrowIfNull(selectedEmails);

		var response = await _validLocationTableClient.GetEntityAsync<TableEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		var source = response.Value;
		if (!string.Equals(source.ETag.ToString(), expectedETag, StringComparison.Ordinal))
		{
			throw new InvalidOperationException("This location changed after the page was loaded. Reload it and review the current users before trying again.");
		}

		var sourceUsers = ParseUsers(GetProperty<string>(source, "users"));
		var selected = selectedEmails
			.Select(email => email.Trim())
			.Where(email => email.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		ValidateSelectedUsers(sourceUsers, selected);

		var selectedSet = new HashSet<string>(selected, StringComparer.OrdinalIgnoreCase);
		var selectedInSourceOrder = sourceUsers.Where(selectedSet.Contains).ToList();
		var remainingUsers = sourceUsers.Where(email => !selectedSet.Contains(email)).ToList();

		var candidateRowKey = await FindCandidateRowKeyAsync(selectedInSourceOrder, cancellationToken);
		if (string.IsNullOrWhiteSpace(candidateRowKey)
			|| await ValidLocationExistsAsync(partitionKey, candidateRowKey, cancellationToken))
		{
			candidateRowKey = Guid.NewGuid().ToString();
		}

		for (var attempt = 0; attempt < 5; attempt++)
		{
			var destination = CloneEntity(source);
			destination.PartitionKey = source.PartitionKey;
			destination.RowKey = candidateRowKey;
			destination["users"] = string.Join(',', selectedInSourceOrder);
			source["users"] = string.Join(',', remainingUsers);

			try
			{
				await _validLocationTableClient.SubmitTransactionAsync(
					[
						new(TableTransactionActionType.UpdateReplace, source, source.ETag),
						new(TableTransactionActionType.Add, destination)
					],
					cancellationToken);
				return $"{partitionKey}|{candidateRowKey}";
			}
			catch (RequestFailedException ex) when (ex.Status == 409 && attempt < 4)
			{
				candidateRowKey = Guid.NewGuid().ToString();
			}
		}

		throw new InvalidOperationException("A separate location could not be created because the destination key is repeatedly in use. Reload the page and try again.");
	}

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
		validLocationEntity.disabled = null;
		validLocationEntity.channel = string.IsNullOrWhiteSpace(validLocation.channel) ? default : validLocation.channel.Trim().ToLower();
		validLocationEntity.zipCode = validLocation.zipCode?.Trim();
		validLocationEntity.lang = validLocation.lang?.Trim().ToLower();
		validLocationEntity.timezone = validLocation.timezone?.Trim();
		validLocationEntity.offset = Normalize(validLocation.offset);
		validLocationEntity.temperatureUnit = NormalizeTemperatureUnit(validLocation.temperatureUnit);
		validLocationEntity.PartitionKey = Capitalize(validLocation.country);
		validLocationEntity.RowKey = validLocation.RowKey;

		validLocationEntity.utc_offset_minutes = DeriveUtcOffsetMinutes(validLocation.timezone, validLocation.offset);
		validLocationEntity.hemisphere = DeriveHemisphere(validLocation.coordinates);

		await _validLocationTableClient.AddEntityAsync(validLocationEntity, cancellationToken: cancellationToken);

		return true;
	}

	internal async Task<bool> UpdateValidLocationAsync(Location? validLocation, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(validLocation);

		var (partitionKey, rowKey) = validLocation.Id.ToKeys();
		var newPartitionKey = Capitalize(validLocation.country);

		var entity = await _validLocationTableClient.GetEntityAsync<LocationEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		var validLocationEntity = entity.Value;
		var originalETag = validLocationEntity.ETag;

		validLocationEntity.city = validLocation.city.Trim();
		validLocationEntity.country = Capitalize(validLocation.country);
		validLocationEntity.coordinates = validLocation.coordinates.Replace(" ", "");
		validLocationEntity.users = DeduplicateUsers(validLocation.users.Trim());
		validLocationEntity.uat = validLocation.uat ? true : validLocationEntity.uat.HasValue ? false : null;
		validLocationEntity.disabled = validLocation.disabled ? true : validLocationEntity.disabled.HasValue ? false : null;
		validLocationEntity.channel = string.IsNullOrWhiteSpace(validLocation.channel) ? default : validLocation.channel.Trim().ToLower();
		validLocationEntity.zipCode = validLocation.zipCode?.Trim();
		validLocationEntity.lang = validLocation.lang?.Trim().ToLower();
		validLocationEntity.timezone = validLocation.timezone?.Trim();
		validLocationEntity.offset = Normalize(validLocation.offset);
		validLocationEntity.PartitionKey = newPartitionKey;
		validLocationEntity.temperatureUnit = NormalizeTemperatureUnit(validLocation.temperatureUnit);
		validLocationEntity.RowKey = validLocation.RowKey;

		validLocationEntity.utc_offset_minutes = DeriveUtcOffsetMinutes(validLocation.timezone, validLocation.offset);
		validLocationEntity.hemisphere = DeriveHemisphere(validLocation.coordinates);

		if (string.Equals(partitionKey, newPartitionKey, StringComparison.OrdinalIgnoreCase))
		{
			await _validLocationTableClient.UpdateEntityAsync(validLocationEntity, originalETag, cancellationToken: cancellationToken);
		}
		else
		{
			// When the partition key (country) changes, we must add a new entity and delete the old one.
			// Azure Table Storage does not support cross-partition transactions, so this is not atomic:
			// the new entity is added first to avoid data loss if the delete fails.
			await _validLocationTableClient.AddEntityAsync(validLocationEntity, cancellationToken: cancellationToken);
			await _validLocationTableClient.DeleteEntityAsync(partitionKey, rowKey, originalETag, cancellationToken: cancellationToken);
		}

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
			var lang = validLocationEntity.lang;
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
						lang,
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

		var normalizedEmail = email.Trim();
		var matches = new List<Location>();

		await foreach (var validLocationEntity in _validLocationTableClient.QueryAsync<LocationEntity>(_ => true, cancellationToken: cancellationToken))
		{
			if (string.IsNullOrWhiteSpace(validLocationEntity.users))
			{
				continue;
			}

			var userEntries = validLocationEntity.users
				.Split(',', StringSplitOptions.RemoveEmptyEntries)
				.Select(entry => entry.Trim());

			if (userEntries.Any(entry => string.Equals(entry, normalizedEmail, StringComparison.OrdinalIgnoreCase)))
			{
				matches.Add(EntityToModel(validLocationEntity));
			}
		}

		return matches;
	}

	internal async Task<IList<Location>> FindLocationsByUserAsync(string email, CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(email);

		var normalizedEmail = email.Trim();
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

	internal async Task<bool> RemoveUserFromValidLocationAsync(Location validLocation, string email, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(validLocation);
		ArgumentException.ThrowIfNullOrWhiteSpace(email);

		var normalizedEmail = email.Trim();
		var (partitionKey, rowKey) = validLocation.Id.ToKeys();
		if (string.IsNullOrWhiteSpace(partitionKey) || string.IsNullOrWhiteSpace(rowKey))
		{
			return false;
		}
		var response = await _validLocationTableClient.GetEntityAsync<LocationEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		var entity = response.Value;

		if (string.IsNullOrWhiteSpace(entity.users))
		{
			return false;
		}

		var entries = entity.users
			.Split(',', StringSplitOptions.RemoveEmptyEntries)
			.Select(entry => entry.Trim())
			.ToList();
		var originalCount = entries.Count;
		var removed = entries.RemoveAll(entry => string.Equals(entry, normalizedEmail, StringComparison.OrdinalIgnoreCase));
		if (removed == 0)
		{
			return false;
		}

		var entityPartitionKey = entity.PartitionKey ?? partitionKey;
		var entityRowKey = entity.RowKey ?? rowKey;

		if (originalCount == removed)
		{
			await _validLocationTableClient.DeleteEntityAsync(entityPartitionKey, entityRowKey, entity.ETag, cancellationToken: cancellationToken);
			return true;
		}

		entity.users = entries.Count == 0 ? string.Empty : string.Join(',', entries);
		entity.PartitionKey = entityPartitionKey;
		entity.RowKey = entityRowKey;
		await _validLocationTableClient.UpdateEntityAsync(entity, entity.ETag, cancellationToken: cancellationToken);
		return true;
	}

	internal async Task<bool> ClearLocationUsersAsync(Location location, string email, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(location);
		ArgumentException.ThrowIfNullOrWhiteSpace(email);

		var normalizedEmail = email.Trim();
		var (partitionKey, rowKey) = location.Id.ToKeys();
		if (string.IsNullOrWhiteSpace(partitionKey) || string.IsNullOrWhiteSpace(rowKey))
		{
			return false;
		}
		var response = await _locationTableClient.GetEntityAsync<LocationEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
		var entity = response.Value;
		var entityPartitionKey = entity.PartitionKey ?? partitionKey;
		var entityRowKey = entity.RowKey ?? rowKey;

		if (!string.Equals(entity.users?.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		entity.users = string.Empty;
		entity.PartitionKey = entityPartitionKey;
		entity.RowKey = entityRowKey;
		await _locationTableClient.UpdateEntityAsync(entity, entity.ETag, cancellationToken: cancellationToken);
		return true;
	}

	internal static IReadOnlyList<string> ParseUsers(string? users)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		return (users ?? string.Empty)
			.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
			.Where(email => seen.Add(email))
			.ToList();
	}

	internal static void ValidateSelectedUsers(IReadOnlyList<string> sourceUsers, IReadOnlyList<string> selectedUsers)
	{
		if (selectedUsers.Count == 0)
		{
			throw new ArgumentException("Select at least one user to move.");
		}

		var sourceSet = sourceUsers.ToHashSet(StringComparer.OrdinalIgnoreCase);
		if (selectedUsers.Any(email => !sourceSet.Contains(email)))
		{
			throw new ArgumentException("The user list changed. Reload this page and review the available users.");
		}

		if (selectedUsers.Count == sourceUsers.Count)
		{
			throw new ArgumentException("At least one user must remain on the source location.");
		}
	}

	internal static TableEntity CloneEntity(TableEntity source)
	{
		var clone = new TableEntity(source.PartitionKey, source.RowKey);
		foreach (var property in source)
		{
			clone[property.Key] = property.Value;
		}
		return clone;
	}

	private async Task<string?> FindCandidateRowKeyAsync(
		IEnumerable<string> selectedEmails,
		CancellationToken cancellationToken)
	{
		foreach (var selectedEmail in selectedEmails)
		{
			await foreach (var user in _userTableClient.QueryAsync<UserEntity>(_ => true, cancellationToken: cancellationToken))
			{
				if (string.Equals(user.email?.Trim(), selectedEmail.Trim(), StringComparison.OrdinalIgnoreCase))
				{
					return string.IsNullOrWhiteSpace(user.PartitionKey) ? null : user.PartitionKey;
				}
			}
		}

		return null;
	}

	private async Task<bool> ValidLocationExistsAsync(
		string partitionKey,
		string rowKey,
		CancellationToken cancellationToken)
	{
		try
		{
			await _validLocationTableClient.GetEntityAsync<TableEntity>(
				partitionKey,
				rowKey,
				cancellationToken: cancellationToken);
			return true;
		}
		catch (RequestFailedException ex) when (ex.Status == 404)
		{
			return false;
		}
	}

	private static T? GetProperty<T>(TableEntity entity, string name)
	{
		return entity.TryGetValue(name, out var value) && value is T typedValue
			? typedValue
			: default;
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
			disabled = locationEntity.disabled == true,
			channel = locationEntity.channel ?? "",
			zipCode = locationEntity.zipCode ?? "",
			lang = locationEntity.lang ?? "",
			timezone = locationEntity.timezone ?? "",
			offset = locationEntity.offset ?? "",
			utc_offset_minutes = locationEntity.utc_offset_minutes,
			preferred_local_time_minutes = locationEntity.preferred_local_time_minutes,
			hemisphere = locationEntity.hemisphere,
			temperatureUnit = locationEntity.temperatureUnit ?? "",
			PartitionKey = locationEntity.PartitionKey ?? "",
			RowKey = locationEntity.RowKey ?? "",
			Timestamp = locationEntity.Timestamp,
		};
	}

	private static string Capitalize(string s) => s.Trim()[0].ToString().ToUpper() + s.Trim()[1..].ToLower();

	private static string DeduplicateUsers(string users)
	{
		if (string.IsNullOrWhiteSpace(users)) return users;
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var unique = users.Split(',', StringSplitOptions.RemoveEmptyEntries)
			.Select(e => e.Trim())
			.Where(e => e.Length > 0 && seen.Add(e));
		return string.Join(',', unique);
	}
	
	private static string? NormalizeTemperatureUnit(string? s)
		=> "F".Equals(s?.Trim(), StringComparison.OrdinalIgnoreCase) ? "F" : "C";

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

	internal static int? DeriveUtcOffsetMinutes(string? timezone, string? offset)
	{
		// Try IANA timezone id first
		if (!string.IsNullOrWhiteSpace(timezone))
		{
			try
			{
				var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone.Trim());
				var span = tz.GetUtcOffset(DateTimeOffset.UtcNow);
				return (int)span.TotalMinutes;
			}
			catch (TimeZoneNotFoundException) { }
			catch (InvalidTimeZoneException) { }
		}

		// Fall back to offset string (e.g. "+02:00", "-05:30", "−08:00")
		if (!string.IsNullOrWhiteSpace(offset))
		{
			var trimmed = offset.Trim();
			var sign = trimmed.StartsWith('-') || trimmed.StartsWith('−') ? -1 : 1;
			var parts = trimmed.TrimStart('+', '-', '−').Split(':');
			if (parts.Length >= 1 && int.TryParse(parts[0], out int hours))
			{
				int mins = 0;
				if (parts.Length >= 2) int.TryParse(parts[1], out mins);
				return sign * (hours * 60 + mins);
			}
		}

		return null;
	}

	internal static string? DeriveHemisphere(string? coordinates)
	{
		if (string.IsNullOrWhiteSpace(coordinates))
		{
			return null;
		}

		var parts = coordinates.Trim().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length >= 1 && double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double latitude))
		{
			return latitude >= 0 ? "N" : "S";
		}

		return null;
	}
}

public sealed record ValidLocationMoveSource(
	string PartitionKey,
	string RowKey,
	ETag ETag,
	IReadOnlyList<string> Users);
