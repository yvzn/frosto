using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin.Pages;

public class MapModel : PageModel
{
	private IConfiguration _configuration;

	public MapModel(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public string AzureMapsSubscriptionKey { get; private set; } = "";

	public void OnGet()
	{
		AzureMapsSubscriptionKey = _configuration.GetConnectionString("AzureMaps") ?? "";
	}
}
