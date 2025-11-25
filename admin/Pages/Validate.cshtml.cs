using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace admin.Pages;

public class ValidateModel(
	LocationService locationService,
	GeographicalDataService geographicalDataService,
	SignUpService signUpService,
	ILogger<ValidateModel> logger) : PageModel
{
	public Location Location { get; set; } = new();

	[BindProperty]
	public Location ValidLocation { get; set; } = new();

	public string? ValidLocationExists { get; set; }

	private static readonly string[] ChannelList = ["", "api", "smtp", "default", "tipimail"];

	public SelectList ChannelOptions { get; set; } = new(ChannelList);

	public ICollection<string> CountryList { get; set; } = Array.Empty<string>();

	public ICollection<string> TimezoneList { get; set; } = Array.Empty<string>();

	public async Task OnGetAsync(string id)
	{
		Location = await locationService.GetLocationAsync(id, HttpContext.RequestAborted) ?? new();
		ValidLocation = await locationService.GetLocationAsync(id, HttpContext.RequestAborted) ?? new();

		var existingLocation = await locationService.FindValidLocationAsync(
			Capitalize(ValidLocation.city),
			Capitalize(ValidLocation.country),
			HttpContext.RequestAborted);
		ValidLocationExists = existingLocation?.Id;

		CountryList = geographicalDataService.GetCountryList();
		TimezoneList = geographicalDataService.GetCommonTimezones();
	}

	private static string Capitalize(string s)
	{
		var trimmed = s.Trim();

		if (string.IsNullOrWhiteSpace(trimmed))
		{
			return trimmed;
		}

		if (trimmed.Length == 1)
		{
			return trimmed.ToUpper();
		}

		return char.ToUpper(trimmed[0]) + trimmed[1..].ToLower();
	}

	public async Task<IActionResult> OnPostAsync()
	{
		if (ValidLocation?.Id is string id)
		{
			Location = await locationService.GetLocationAsync(id, HttpContext.RequestAborted) ?? new();
		}

		if (!ModelState.IsValid)
		{
			return Page();
		}

		logger.LogInformation("Saving location {LocationId}", ValidLocation?.Id);
		await locationService.CreateValidLocationAsync(ValidLocation, HttpContext.RequestAborted);
		await signUpService.DeleteSignUpAsync(ValidLocation?.Id, HttpContext.RequestAborted);

		return RedirectToPage("./Index", new { m = $"Updated {ValidLocation?.city} ({ValidLocation?.RowKey})" });
	}

	public async Task<IActionResult> OnPostDiscardAsync()
	{
		logger.LogInformation("Discarding sign-up {LocationId}", ValidLocation?.Id);
		await signUpService.DeleteSignUpAsync(ValidLocation?.Id, HttpContext.RequestAborted);
		return RedirectToPage("./Index", new { m = $"Sign-Up discarded {ValidLocation?.city} ({ValidLocation?.RowKey})" });
	}
}
