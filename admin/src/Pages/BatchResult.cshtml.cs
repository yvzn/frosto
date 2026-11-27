using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace admin.Pages;

public class BatchResultModel : PageModel
{
	private readonly BatchService _batchService;

	public BatchResultModel(BatchService batchService)
	{
		_batchService = batchService;
	}

	public BatchStats Stats { get; private set; } = new();
	public BatchBuildDiagnostics? BuildDiagnostics { get; private set; }

	[TempData]
	public string? BatchBuildDiagnosticsJson { get; set; }

	public async Task OnGetAsync()
	{
		Stats = await _batchService.GetBatchStatsAsync(HttpContext.RequestAborted);
		if (!string.IsNullOrEmpty(BatchBuildDiagnosticsJson))
		{
			BuildDiagnostics = JsonSerializer.Deserialize<BatchBuildDiagnostics>(BatchBuildDiagnosticsJson);
		}
	}
}
