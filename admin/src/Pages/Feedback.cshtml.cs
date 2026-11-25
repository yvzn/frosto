using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace admin.Pages;

public class FeedbackModel(FeedbackService feedbackService) : PageModel
{
	public IList<Feedback> Feedback { get; private set; } = [];
	public string? SelectedSentiment { get; private set; }
	public int PositiveCount { get; private set; }
	public int NegativeCount { get; private set; }
	public Feedback? DeletedFeedback { get; private set; }

	[TempData]
	public string? DeletedFeedbackJson { get; set; }

	public async Task OnGetAsync(string? sentiment)
	{
		SelectedSentiment = sentiment is "positive" or "negative" ? sentiment : null;
		var entries = await feedbackService.GetFeedbackAsync(HttpContext.RequestAborted);
		PositiveCount = entries.Count(x => x.Sentiment == "positive");
		NegativeCount = entries.Count(x => x.Sentiment == "negative");
		Feedback = [.. entries
			.Where(x => SelectedSentiment is null || x.Sentiment == SelectedSentiment)
			.OrderByDescending(x => x.Timestamp)];

		if (!string.IsNullOrEmpty(DeletedFeedbackJson))
		{
			DeletedFeedback = JsonSerializer.Deserialize<Feedback>(DeletedFeedbackJson);
		}
	}

	public async Task<IActionResult> OnPostDeleteAsync(string id, string? sentiment)
	{
		var deletedFeedback = await feedbackService.GetFeedbackByIdAsync(id, HttpContext.RequestAborted);
		if (deletedFeedback != null)
		{
			await feedbackService.DeleteAsync(id, HttpContext.RequestAborted);
			DeletedFeedbackJson = JsonSerializer.Serialize(deletedFeedback);
		}

		return RedirectToPage(new { sentiment });
	}

	public async Task<IActionResult> OnPostUndoAsync(Feedback deletedFeedback, string? sentiment)
	{
		if (!string.IsNullOrEmpty(deletedFeedback.Id))
		{
			await feedbackService.RestoreAsync(deletedFeedback, HttpContext.RequestAborted);
		}

		return RedirectToPage(new { sentiment });
	}
}
