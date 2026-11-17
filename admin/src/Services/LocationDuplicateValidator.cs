using admin.Models;

namespace admin.Services;

public static class LocationDuplicateValidator
{
	public static bool RequiresDuplicateConfirmation(Location? location, string? existingLocationId, bool confirmDuplicate)
	{
		if (location is null || string.IsNullOrWhiteSpace(existingLocationId))
		{
			return false;
		}

		if (string.IsNullOrWhiteSpace(location.city) || string.IsNullOrWhiteSpace(location.country))
		{
			return false;
		}

		return !confirmDuplicate;
	}

	public static string GetDuplicateMessage(Location? location, string? existingLocationId)
	{
		var city = Normalize(location?.city);
		var country = Normalize(location?.country);
		var id = existingLocationId ?? string.Empty;

		return $"A valid location already exists for {city} in {country}. Review the existing entry at {id} and confirm again to create this sign-up.";
	}

	private static string Normalize(string? value)
	{
		var trimmed = value?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(trimmed))
		{
			return trimmed;
		}

		if (trimmed.Length == 1)
		{
			return trimmed.ToUpperInvariant();
		}

		return char.ToUpperInvariant(trimmed[0]) + trimmed[1..].ToLowerInvariant();
	}
}
