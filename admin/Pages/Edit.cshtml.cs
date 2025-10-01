using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace admin.Pages;

public class EditModel(LocationService locationService, ILogger<EditModel> logger) : PageModel
{
	private readonly LocationService _locationService = locationService;

	private readonly ILogger<EditModel> _logger = logger;

	[BindProperty]
	public Location ValidLocation { get; set; } = new();

	public SelectList ChannelOptions { get; set; } = new(new[] { "", "api", "smtp", "default", "tipimail" });

	public ICollection<string> CountryList { get; set; } = ["France", "Belgique", "Algérie", "Canada", "United states of america"];

	public async Task OnGetAsync(string id)
	{
		ValidLocation = await _locationService.GetValidLocationAsync(id, HttpContext.RequestAborted) ?? new();
	}

	public async Task<IActionResult> OnPostAsync()
	{
		if (!ModelState.IsValid)
		{
			return Page();
		}

		_logger.LogInformation("Saving valid location {LocationId}", ValidLocation?.Id);
		await _locationService.UpdateValidLocationAsync(ValidLocation, HttpContext.RequestAborted);

		return RedirectToPage("./Index", new { m = $"Updated {ValidLocation?.city} ({ValidLocation?.RowKey})" });
	}
}
