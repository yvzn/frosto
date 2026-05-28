using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin.Pages;

public class MonitoringMapModel : PageModel
{
	private readonly IConfiguration _configuration;

	public MonitoringMapModel(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public string AzureMapsSubscriptionKey { get; private set; } = "";

	[BindProperty(SupportsGet = true)]
	public int Days { get; set; } = 30;

	public void OnGet()
	{
		AzureMapsSubscriptionKey = _configuration.GetConnectionString("AzureMaps") ?? "";
		if (Days <= 0) Days = 30;
	}
}
