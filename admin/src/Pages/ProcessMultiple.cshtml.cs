using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin.Pages;

public class ProcessMultipleModel(SignUpService signUpService) : PageModel
{
	public IList<SignUp> SelectedItems { get; private set; } = [];

	public IList<DiscardResult> DiscardedItems { get; private set; } = [];

	public string? ErrorMessage { get; private set; }

	public class DiscardResult
	{
		public required SignUp SignUp { get; set; }
		public bool Success { get; set; }
		public string? ErrorMessage { get; set; }
	}

	public async Task<IActionResult> OnPostAsync(string selectedIds)
	{
		if (string.IsNullOrWhiteSpace(selectedIds))
		{
			ErrorMessage = "No items selected.";
			return Page();
		}

		var ids = selectedIds.Split(',', StringSplitOptions.RemoveEmptyEntries);

		if (ids.Length == 0)
		{
			ErrorMessage = "No valid items selected.";
			return Page();
		}

		// Fetch the selected sign-ups
		var allSignUps = await signUpService.GetNewSignUpsAsync(HttpContext.RequestAborted);
		SelectedItems = [.. allSignUps.Where(s => ids.Contains(s.Id))];

		if (SelectedItems.Count == 0)
		{
			ErrorMessage = "Selected items not found.";
			return Page();
		}

		return Page();
	}

	public async Task<IActionResult> OnPostDiscardAsync(string selectedIds)
	{
		if (string.IsNullOrWhiteSpace(selectedIds))
		{
			return Page();
		}

		var ids = selectedIds.Split(',', StringSplitOptions.RemoveEmptyEntries);

		if (ids.Length == 0)
		{
			return Page();
		}

		// Fetch the selected sign-ups before deletion
		var allSignUps = await signUpService.GetNewSignUpsAsync(HttpContext.RequestAborted);
		var itemsToDiscard = allSignUps.Where(s => ids.Contains(s.Id)).ToList();

		if (itemsToDiscard.Count == 0)
		{
			ErrorMessage = "Selected items not found.";
			return Page();
		}

		// Delete each selected sign-up and track results
		var discardResults = new List<DiscardResult>();

		foreach (var item in itemsToDiscard)
		{
			try
			{
				var success = await signUpService.DeleteSignUpAsync(item.Id, HttpContext.RequestAborted);
				discardResults.Add(new DiscardResult
				{
					SignUp = item,
					Success = success,
					ErrorMessage = success ? null : "Delete operation returned error"
				});
			}
			catch (Exception ex)
			{
				discardResults.Add(new DiscardResult
				{
					SignUp = item,
					Success = false,
					ErrorMessage = ex.Message
				});
			}
		}

		DiscardedItems = discardResults;
		return Page();
	}
}
