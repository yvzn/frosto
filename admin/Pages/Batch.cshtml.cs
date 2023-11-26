using admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin.Pages;

public class BatchModel : PageModel
{
	[BindProperty]
	public BatchConfig BatchConfig { get; set; } = new();

	public async Task<IActionResult> OnPostAsync()
	{
		if (!ModelState.IsValid)
		{
			return Page();
		}

		// TODO
		return Page();
	}
}
