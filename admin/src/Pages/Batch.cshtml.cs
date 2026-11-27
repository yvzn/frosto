using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace admin.Pages;

public class BatchModel : PageModel
{
	private readonly BatchService _batchService;

	public BatchModel(BatchService batchService)
	{
		_batchService = batchService;
	}

	[BindProperty]
	public BatchConfig BatchConfig { get; set; } = new();

	[BindProperty]
	public bool RebuildAllBatches { get; set; } = true;

	[TempData]
	public string? BatchBuildDiagnosticsJson { get; set; }

	public async Task<IActionResult> OnPostAsync()
	{
		if (!ModelState.IsValid)
		{
			return Page();
		}

		var result = await _batchService.BuildBatches(
			BatchConfig.periodInDays,
			BatchConfig.capacityGuardMultiplier,
			RebuildAllBatches,
			HttpContext.RequestAborted);
		BatchBuildDiagnosticsJson = JsonSerializer.Serialize(result.Diagnostics);
		return RedirectToPage("./BatchResult");
	}
}
