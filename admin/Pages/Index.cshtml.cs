using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin.Pages;

public class IndexModel : PageModel
{
	private readonly ILogger<IndexModel> _logger;
	private ILocationService _locationService;

	public IndexModel(ILogger<IndexModel> logger, ILocationService locationService)
	{
		_logger = logger;
		_locationService = locationService;
	}

	public IList<Location> ValidLocations { get; private set; } = Array.Empty<Location>();

	public async Task OnGetAsync()
	{
		ValidLocations = await _locationService.GetValidLocations();
	}
}
