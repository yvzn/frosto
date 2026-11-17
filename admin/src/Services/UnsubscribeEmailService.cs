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
		var changes = new List<UnsubscribeChange>();

		if (validLocation is not null)
		{
			var removed = await _locationService.RemoveUserFromValidLocationAsync(validLocation, normalizedEmail, cancellationToken);
			if (removed)
			{
				changes.Add(new UnsubscribeChange(
					EntityType: "Valid location",
					Action: "Removed from valid location",
					Id: validLocation.Id,
					City: validLocation.city,
					Country: validLocation.country,
					Email: normalizedEmail));
			}
		}

		if (location is not null)
		{
			var cleared = await _locationService.ClearLocationUsersAsync(location, normalizedEmail, cancellationToken);
			if (cleared)
			{
				changes.Add(new UnsubscribeChange(
					EntityType: "Location",
					Action: "Anonymized user from location",
					Id: location.Id,
					City: location.city,
					Country: location.country,
					Email: normalizedEmail));
			}
		}

		if (user is not null)
		{
			var disabled = await _userService.DisableUserAsync(user, cancellationToken);
			if (disabled)
			{
				changes.Add(new UnsubscribeChange(
					EntityType: "User",
					Action: "Anonymized user record",
					Id: user.Id,
					City: null,
					Country: null,
					Email: user.email));
			}
		}

		if (changes.Count == 0)
		{
			return UnsubscribeResult.Failed($"No matching records were updated for {email}.");
		}

		return UnsubscribeResult.Succeeded(changes, $"{email} unsubscribed successfully.");
	}
}

public readonly record struct UnsubscribeResult(bool Success, string Message, IReadOnlyList<UnsubscribeChange> Changes)
{
	public static UnsubscribeResult Succeeded(IReadOnlyList<UnsubscribeChange> changes, string message) => new(true, message, changes);

	public static UnsubscribeResult Failed(string message) => new(false, message, Array.Empty<UnsubscribeChange>());
}

public record UnsubscribeChange(
	string EntityType,
	string Action,
	string? Id,
	string? City,
	string? Country,
	string? Email);
