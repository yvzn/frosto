using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin
{
	public class ValidateModel : PageModel
	{
		private readonly LocationService _locationService;
		private readonly ILogger<ValidateModel> _logger;

		public ValidateModel(LocationService locationService, ILogger<ValidateModel> logger)
		{
			_locationService = locationService;
			_logger = logger;
		}

		public Location Location { get; set; } = new();

		[BindProperty]
		public Location ValidLocation { get; set; } = new();

		public async Task OnGetAsync(string id)
		{
			Location = await _locationService.GetLocationAsync(id, HttpContext.RequestAborted) ?? new();
			ValidLocation = await _locationService.GetLocationAsync(id, HttpContext.RequestAborted) ?? new();
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
			await _locationService.PutValidLocationAsync(ValidLocation, HttpContext.RequestAborted);

			return RedirectToPage("./Index", new { m = $"Updated {ValidLocation?.city} ({ValidLocation?.RowKey})" });
		}
	}
}