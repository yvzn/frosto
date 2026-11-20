using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin.Pages;

public class IndexModel(SignUpService signUpService, GeographicalDataService geographicalDataService) : PageModel
{
	public IList<SignUp> NewSignUps { get; private set; } = Array.Empty<SignUp>();
	public string? Message { get; private set; } = string.Empty;

	public async Task OnGetAsync()
	{
		var countries = await geographicalDataService.GetCountryListAsync(HttpContext.RequestAborted);
		var signUps = await signUpService.GetNewSignUpsAsync(HttpContext.RequestAborted);
		foreach (var signUp in signUps)
		{
			signUp.ReviewNotes = InputReview.GetNotes(signUp.comment, signUp.duration, signUp.country, countries);
		}

		NewSignUps = [.. signUps.OrderByDescending(l => l.Timestamp)];
		Message = Request.Query["m"];
	}
}
