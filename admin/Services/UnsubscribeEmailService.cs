using admin.Models;

namespace admin.Services;

public class UnsubscribeEmailService(LocationService locationService, UserService userService)
{
	private readonly LocationService _locationService = locationService;
	private readonly UserService _userService = userService;

	public async Task<UnsubscribeResult> UnsubscribeAsync(string email, Location? validLocation, Location? location, User? user, CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(email);

		var normalizedEmail = email.Trim();
		var updatedTargets = new List<string>();

		if (validLocation is not null)
		{
			var removed = await _locationService.RemoveUserFromValidLocationAsync(validLocation, normalizedEmail, cancellationToken);
			if (removed)
			{
				updatedTargets.Add($"valid location {validLocation.city}, {validLocation.country} ({validLocation.RowKey})");
			}
		}

		if (location is not null)
		{
			var cleared = await _locationService.ClearLocationUsersAsync(location, normalizedEmail, cancellationToken);
			if (cleared)
			{
				updatedTargets.Add($"location {location.city}, {location.country} ({location.RowKey})");
			}
		}

		if (user is not null)
		{
			var disabled = await _userService.DisableUserAsync(user, cancellationToken);
			if (disabled)
			{
				updatedTargets.Add($"user record {user.email} ({user.RowKey})");
			}
		}

		if (updatedTargets.Count == 0)
		{
			return UnsubscribeResult.Failed($"No matching records were updated for {email}.");
		}

		var summary = string.Join(" – ", updatedTargets);
		return UnsubscribeResult.Succeeded($"{email} unsubscribed from {summary}.");
	}
}

public readonly record struct UnsubscribeResult(bool Success, string Message)
{
	public static UnsubscribeResult Succeeded(string message) => new(true, message);

	public static UnsubscribeResult Failed(string message) => new(false, message);
}
