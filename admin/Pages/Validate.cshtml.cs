using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace admin.Pages;

public class ValidateModel : PageModel
{
	private readonly LocationService _locationService;
	private readonly SignUpService _signUpService;
	private readonly ILogger<ValidateModel> _logger;

	public ValidateModel(LocationService locationService, SignUpService signUpService, ILogger<ValidateModel> logger)
	{
		_locationService = locationService;
		_signUpService = signUpService;
		_logger = logger;
	}

	public Location Location { get; set; } = new();

	[BindProperty]
	public Location ValidLocation { get; set; } = new();

	public string? ValidLocationExists { get; set; }

	public SelectList ChannelOptions { get; set; } = new(new[] { "", "api", "smtp", "default", "tipimail" });

	public ICollection<string> CountryList { get; set; } = ["France", "Belgique", "Algérie", "Canada"];

	public async Task OnGetAsync(string id)
	{
		Location = await _locationService.GetLocationAsync(id, HttpContext.RequestAborted) ?? new();
		ValidLocation = await _locationService.GetLocationAsync(id, HttpContext.RequestAborted) ?? new();

		var existingLocation = await _locationService.FindValidLocationAsync(ValidLocation.city, ValidLocation.country, HttpContext.RequestAborted);
		ValidLocationExists = existingLocation?.Id;
	}

	public async Task<IActionResult> OnPostAsync()
	{
		if (ValidLocation?.Id is string id)
		{
			Location = await _locationService.GetLocationAsync(id, HttpContext.RequestAborted) ?? new();
		}

		if (!ModelState.IsValid)
		{
			return Page();
		}

		_logger.LogInformation("Saving location {LocationId}", ValidLocation?.Id);
		await _locationService.CreateValidLocationAsync(ValidLocation, HttpContext.RequestAborted);
		await _signUpService.DeleteSignUpAsync(ValidLocation?.Id, HttpContext.RequestAborted);

		return RedirectToPage("./Index", new { m = $"Updated {ValidLocation?.city} ({ValidLocation?.RowKey})" });
	}

	public async Task<IActionResult> OnPostDiscardAsync() {
		_logger.LogInformation("Discarding sign-up {LocationId}", ValidLocation?.Id);
		await _signUpService.DeleteSignUpAsync(ValidLocation?.Id, HttpContext.RequestAborted);
		return RedirectToPage("./Index", new { m = $"Sign-Up discarded {ValidLocation?.city} ({ValidLocation?.RowKey})" });
	}
}
