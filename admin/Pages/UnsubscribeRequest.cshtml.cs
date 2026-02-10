using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin.Pages;

public class UnsubscribeRequestModel(
	UnsubscribeRequestService unsubscribeRequestService,
	JwtValidationService jwtValidationService) : PageModel
{
	public IList<Unsubscribe> UnsubscribeRequests { get; private set; } = [];

	[BindProperty]
	public Unsubscribe? CurrentRequest { get; set; }

	public Unsubscribe? DeletedRequest { get; set; }
	public bool ShowDeletionSummary { get; set; }

	public string? ErrorMessage { get; set; }
	public bool IsProcessing { get; set; }
	public bool SignatureValid { get; set; }
	public Dictionary<string, string> TokenClaims { get; set; } = [];
	public string? Algorithm { get; set; }

	public async Task OnGetAsync()
	{
		await LoadUnsubscribeRequestsAsync(HttpContext.RequestAborted);
	}

	public async Task<IActionResult> OnGetProcessAsync(string id)
	{
		IsProcessing = true;

		// Load the request from database
		CurrentRequest = await unsubscribeRequestService.GetUnsubscribeRequestByIdAsync(id, HttpContext.RequestAborted);
		if (CurrentRequest == null)
		{
			ErrorMessage = "Request not found.";
			return Page();
		}

		var token = CurrentRequest.user;
		if (string.IsNullOrEmpty(token))
		{
			ErrorMessage = "Token is empty.";
			return Page();
		}

		// Validate JWT using the dedicated service
		var validationResult = await jwtValidationService.ValidateTokenAsync(token, HttpContext.RequestAborted);

		if (!string.IsNullOrEmpty(validationResult.Error))
		{
			ErrorMessage = validationResult.Error;
			SignatureValid = validationResult.SignatureValid;
			Algorithm = validationResult.Algorithm;
			return Page();
		}

		SignatureValid = validationResult.SignatureValid;
		Algorithm = validationResult.Algorithm;
		TokenClaims = validationResult.Claims;

		return Page();
	}

	public async Task<IActionResult> OnPostCompleteAsync()
	{
		if (CurrentRequest == null || string.IsNullOrEmpty(CurrentRequest.Id))
		{
			return RedirectToPage();
		}

		// Store the request info before deletion
		DeletedRequest = await unsubscribeRequestService.GetUnsubscribeRequestByIdAsync(CurrentRequest.Id, HttpContext.RequestAborted);

		if (DeletedRequest != null)
		{
			await unsubscribeRequestService.DeleteUnsubscribeRequestAsync(CurrentRequest.Id, HttpContext.RequestAborted);
			ShowDeletionSummary = true;
		}

		return Page();
	}

	private async Task LoadUnsubscribeRequestsAsync(CancellationToken cancellationToken)
	{
		var unsubscribes = await unsubscribeRequestService.GetUnsubscribeRequestsAsync(cancellationToken);
		UnsubscribeRequests = [.. unsubscribes.OrderByDescending(x => x.Timestamp)];
	}
}
