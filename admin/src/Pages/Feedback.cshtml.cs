using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin.Pages;

public class FeedbackModel(FeedbackService feedbackService) : PageModel
{
	public IList<Feedback> Feedback { get; private set; } = [];
	public string? SelectedSentiment { get; private set; }
	public int PositiveCount { get; private set; }
	public int NegativeCount { get; private set; }

	public async Task OnGetAsync(string? sentiment)
	{
		SelectedSentiment = sentiment is "positive" or "negative" ? sentiment : null;
		var entries = await feedbackService.GetFeedbackAsync(HttpContext.RequestAborted);
		PositiveCount = entries.Count(x => x.Sentiment == "positive");
		NegativeCount = entries.Count(x => x.Sentiment == "negative");
		Feedback = [.. entries
			.Where(x => SelectedSentiment is null || x.Sentiment == SelectedSentiment)
			.OrderByDescending(x => x.Timestamp)];
	}

	public async Task<IActionResult> OnPostDeleteAsync(string id)
	{
		await feedbackService.DeleteAsync(id, HttpContext.RequestAborted);
		return RedirectToPage();
	}
}
