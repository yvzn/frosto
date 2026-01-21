using System.ComponentModel.DataAnnotations;
using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin.Pages;

public class UnsubscribeEmailModel(LocationService locationService, UnsubscribeService unsubscribeService, UserService userService) : PageModel
{
	[BindProperty]
	[EmailAddress]
	[Required]
	public string Email { get; set; } = string.Empty;

	[BindProperty]
	public string? SelectedValidLocationId { get; set; }

	[BindProperty]
	public string? SelectedLocationId { get; set; }

	[BindProperty]
	public string? SelectedUserId { get; set; }

	public bool EmailSubmitted { get; private set; }
	public IList<Location> MatchingValidLocations { get; private set; } = [];
	public IList<Location> MatchingLocations { get; private set; } = [];
	public IList<User> MatchingUsers { get; private set; } = [];
	public string? ErrorMessage { get; private set; }

	public void OnGet()
	{
	}

	public async Task<IActionResult> OnPostAsync()
	{
		if (!ModelState.IsValid)
		{
			return Page();
		}

		Email = Email.Trim();
		EmailSubmitted = true;
		SelectedValidLocationId = null;
		SelectedLocationId = null;
		SelectedUserId = null;
		ErrorMessage = null;

		await LoadMatchesAsync(HttpContext.RequestAborted);

		return Page();
	}

	public async Task<IActionResult> OnPostSelectAsync()
	{
		if (string.IsNullOrWhiteSpace(Email))
		{
			ModelState.AddModelError(nameof(Email), "Email address is required to continue.");
			EmailSubmitted = false;
			return Page();
		}

		Email = Email.Trim();
		EmailSubmitted = true;
		ErrorMessage = null;
		await LoadMatchesAsync(HttpContext.RequestAborted);

		var confirmedValidLocation = GetSelectedLocation(MatchingValidLocations, SelectedValidLocationId, nameof(SelectedValidLocationId), "Select a valid location entry to continue.");
		var confirmedLocation = GetSelectedLocation(MatchingLocations, SelectedLocationId, nameof(SelectedLocationId), "Select a location entry to continue.");
		var confirmedUser = GetSelectedUser(MatchingUsers, SelectedUserId, nameof(SelectedUserId));

		if (!ModelState.IsValid)
		{
			return Page();
		}

		var result = await unsubscribeService.UnsubscribeAsync(Email, confirmedValidLocation, confirmedLocation, confirmedUser, HttpContext.RequestAborted);
		if (result.Success)
		{
			return RedirectToPage("/Index", new { m = result.Message });
		}

		ErrorMessage = result.Message;
		return Page();
	}

	private async Task LoadMatchesAsync(CancellationToken cancellationToken)
	{
		MatchingValidLocations = [.. (await locationService.FindValidLocationsByUserAsync(Email, cancellationToken))
			.OrderBy(location => location.country).ThenBy(location => location.city)];
		MatchingLocations = [.. (await locationService.FindLocationsByUserAsync(Email, cancellationToken))
			.OrderBy(location => location.country).ThenBy(location => location.city)];
		MatchingUsers = [.. (await userService.FindUsersByEmailAsync(Email, cancellationToken))
			.OrderBy(user => user.email)];
	}

	private Location? GetSelectedLocation(IList<Location> items, string? selectedId, string propertyName, string selectionMessage)
	{
		if (items.Count == 0)
		{
			return null;
		}

		if (string.IsNullOrWhiteSpace(selectedId))
		{
			ModelState.AddModelError(propertyName, selectionMessage);
			return null;
		}

		var match = items.FirstOrDefault(item => item.Id == selectedId);
		if (match is null)
		{
			ModelState.AddModelError(propertyName, "The selected entry is no longer available.");
		}

		return match;
	}

	private User? GetSelectedUser(IList<User> items, string? selectedId, string propertyName)
	{
		if (items.Count == 0)
		{
			return null;
		}

		if (string.IsNullOrWhiteSpace(selectedId))
		{
			ModelState.AddModelError(propertyName, "Select a user entry to continue.");
			return null;
		}

		var match = items.FirstOrDefault(item => item.Id == selectedId);
		if (match is null)
		{
			ModelState.AddModelError(propertyName, "The selected user entry is no longer available.");
		}

		return match;
	}
}
