using admin.Services;
using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin.Pages;

public class MoveUsersModel(LocationService locationService) : PageModel
{
	private readonly LocationService _locationService = locationService;

	public string SourceId { get; private set; } = string.Empty;

	public IReadOnlyList<string> Users { get; private set; } = Array.Empty<string>();

	[BindProperty]
	public string ExpectedETag { get; set; } = string.Empty;

	[BindProperty]
	public IList<string> SelectedEmails { get; set; } = [];

	public async Task<IActionResult> OnGetAsync(string id)
	{
		SourceId = id;
		try
		{
			await LoadSourceAsync(id);
			return Page();
		}
		catch (RequestFailedException ex) when (ex.Status == 404)
		{
			return NotFound();
		}
	}

	public async Task<IActionResult> OnPostAsync(string id)
	{
		SourceId = id;
		try
		{
			var destinationId = await _locationService.MoveValidLocationUsersAsync(
				id,
				ExpectedETag,
				SelectedEmails,
				HttpContext.RequestAborted);

			return RedirectToPage("./Edit", new
			{
				id = destinationId,
				m = $"Moved {SelectedEmails.Count} user(s) from {id} to {destinationId}. Batch scheduling must be updated separately."
			});
		}
		catch (ArgumentException ex)
		{
			ModelState.AddModelError(nameof(SelectedEmails), ex.Message);
		}
		catch (InvalidOperationException ex)
		{
			ModelState.AddModelError(string.Empty, ex.Message);
		}
		catch (RequestFailedException ex)
		{
			ModelState.AddModelError(string.Empty, ex.Status == 412
				? "This location changed while the move was being saved. Reload and try again."
				: "The move could not be saved. Neither location was changed; reload and try again.");
		}

		SelectedEmails = [];
		try
		{
			await LoadSourceAsync(id);
		}
		catch (RequestFailedException ex) when (ex.Status == 404)
		{
			ModelState.AddModelError(string.Empty, "The source location no longer exists.");
		}
		return Page();
	}

	private async Task LoadSourceAsync(string id)
	{
		var source = await _locationService.GetValidLocationMoveSourceAsync(id, HttpContext.RequestAborted);
		Users = source.Users;
		ExpectedETag = source.ETag.ToString();
	}
}
