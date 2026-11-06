using System.ComponentModel.DataAnnotations;
using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin.Pages;

public class FindByEmailModel(LocationService locationService) : PageModel
{
	[BindProperty(SupportsGet = true)]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	public bool EmailSubmitted { get; private set; }

	public IList<Location> MatchingLocations { get; private set; } = [];

	public async Task OnGetAsync()
	{
		var normalizedEmail = Email.Trim();
		EmailSubmitted = !string.IsNullOrWhiteSpace(normalizedEmail);
		if (!EmailSubmitted)
		{
			MatchingLocations = [];
			return;
		}

		Email = normalizedEmail;
		MatchingLocations = [.. (await locationService.FindValidLocationsByUserAsync(Email, HttpContext.RequestAborted))
			.OrderBy(location => location.country).ThenBy(location => location.city)];
	}
}
