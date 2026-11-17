using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin.Pages;

public class BatchResultModel : PageModel
{
	private readonly BatchService _batchService;

	public BatchResultModel(BatchService batchService)
	{
		_batchService = batchService;
	}

	public BatchStats Stats { get; private set; } = new();

	public async Task OnGetAsync()
	{
		Stats = await _batchService.GetBatchStatsAsync(HttpContext.RequestAborted);
	}
}
