using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin.Pages;

public class CheckSubscriptionModel(
	CheckSubscriptionService checkSubscriptionService,
	LocationService locationService,
	ILogger<CheckSubscriptionModel> logger) : PageModel
{
	[BindProperty]
	public CheckSubscription? SelectedRequest { get; set; }

	public IList<CheckSubscription> CheckSubscriptionRequests { get; private set; } = [];

	public IList<Location> FoundLocations { get; private set; } = [];

	public string? SearchError { get; private set; }

	public bool ShowResults { get; private set; }

	public async Task OnGetAsync()
	{
		await LoadCheckSubscriptionRequestsAsync(HttpContext.RequestAborted);
	}

	public async Task<IActionResult> OnPostSearchAsync()
	{
		if (SelectedRequest == null || string.IsNullOrWhiteSpace(SelectedRequest.Id))
		{
			return Page();
		}

		// Load the selected request from the list
		await LoadCheckSubscriptionRequestsAsync(HttpContext.RequestAborted);
		var selected = CheckSubscriptionRequests.FirstOrDefault(r => r.Id == SelectedRequest.Id);
		if (selected == null)
		{
			return Page();
		}

		SelectedRequest = selected;

		// Search for locations with this email
		try
		{
			if (string.IsNullOrWhiteSpace(SelectedRequest.email))
			{
				SearchError = "No email address found in the request.";
				ShowResults = true;
				return Page();
			}

			FoundLocations = await locationService.FindValidLocationsByUserAsync(SelectedRequest.email, HttpContext.RequestAborted);

			if (FoundLocations.Count == 0)
			{
				SearchError = $"No active locations found for {SelectedRequest.email}. User is not subscribed to any service.";
			}

			ShowResults = true;
			logger.LogInformation("Searched locations for email {Email}, found {Count} locations", SelectedRequest.email, FoundLocations.Count);
		}
		catch (Exception ex)
		{
			SearchError = $"An error occurred while searching: {ex.Message}";
			logger.LogError(ex, "Error searching locations for email {Email}", SelectedRequest.email);
			ShowResults = true;
		}

		return Page();
	}

	public async Task<IActionResult> OnPostCompleteAsync()
	{
		if (SelectedRequest == null || string.IsNullOrWhiteSpace(SelectedRequest.Id))
		{
			return Page();
		}

		logger.LogInformation("Marking check subscription request as completed {RequestId}", SelectedRequest.Id);
		await checkSubscriptionService.DeleteCheckSubscriptionAsync(SelectedRequest.Id, HttpContext.RequestAborted);

		return RedirectToPage("./CheckSubscription", new { m = $"Request completed and deleted for {SelectedRequest.email}" });
	}

	private async Task LoadCheckSubscriptionRequestsAsync(CancellationToken cancellationToken)
	{
		CheckSubscriptionRequests = await checkSubscriptionService.GetCheckSubscriptionRequestsAsync(cancellationToken);
	}
}
