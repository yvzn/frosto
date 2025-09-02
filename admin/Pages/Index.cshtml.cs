using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin.Pages;

public class IndexModel(SignUpService signUpService) : PageModel
{
	public IList<SignUp> NewSignUps { get; private set; } = Array.Empty<SignUp>();
	public string? Message { get; private set; } = string.Empty;

	public async Task OnGetAsync()
	{
		NewSignUps = [.. (await signUpService.GetNewSignUpsAsync(HttpContext.RequestAborted)).OrderByDescending(l => l.Timestamp)];
		Message = Request.Query["m"];
	}
}
