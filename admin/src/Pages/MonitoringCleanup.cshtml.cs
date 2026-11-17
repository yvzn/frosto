using admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace admin.Pages;

public class MonitoringCleanupModel : PageModel
{
	private readonly MonitoringService _monitoringService;

	public MonitoringCleanupModel(MonitoringService monitoringService)
	{
		_monitoringService = monitoringService;
	}

	[BindProperty]
	[Required]
	[Range(1, int.MaxValue)]
	[DisplayName("Days")]
	public int OlderThanDays { get; set; } = 90;

	public async Task<IActionResult> OnPostAsync()
	{
		if (!ModelState.IsValid)
		{
			return Page();
		}

		var deleted = await _monitoringService.DeleteOldMonitoringRecordsAsync(OlderThanDays, HttpContext.RequestAborted);
		return RedirectToPage("./Index", new { m = $"{deleted} monitoring record(s) deleted" });
	}
}
