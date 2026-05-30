using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

	public async Task<IActionResult> OnPostAsync()
	{
		if (!ModelState.IsValid)
		{
			return Page();
		}

		await _batchService.DeleteAllBatches(HttpContext.RequestAborted);
		await _batchService.CreateBatches(BatchConfig.periodInDays, BatchConfig.capacityGuardMultiplier, HttpContext.RequestAborted);
		return RedirectToPage("./BatchResult");
	}
}
