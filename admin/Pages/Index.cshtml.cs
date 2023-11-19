using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin.Pages;

public class IndexModel : PageModel
{
	private SignUpService _signUpService;

	public IndexModel(SignUpService signUpService)
	{
		_signUpService = signUpService;
	}

	public IEnumerable<SignUp> NewSignUps { get; private set; } = Array.Empty<SignUp>();
	public string? Message { get; private set; } = string.Empty;

	public async Task OnGetAsync()
	{
		NewSignUps = (await _signUpService.GetNewSignUpsAsync(HttpContext.RequestAborted)).OrderByDescending(l => l.Timestamp).ToList();
		Message = Request.Query["m"];
	}
}
