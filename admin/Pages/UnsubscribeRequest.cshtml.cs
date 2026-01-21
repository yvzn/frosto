using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin.Pages;

public class UnsubscribeRequestModel(
	UnsubscribeRequestService unsubscribeRequestService) : PageModel
{
	public IList<Unsubscribe> UnsubscribeRequests { get; private set; } = [];

	public async Task OnGetAsync()
	{
		await LoadUnsubscribeRequestsAsync(HttpContext.RequestAborted);
	}

	private async Task LoadUnsubscribeRequestsAsync(CancellationToken cancellationToken)
	{
		UnsubscribeRequests = await unsubscribeRequestService.GetUnsubscribeRequestsAsync(cancellationToken);
	}
}
