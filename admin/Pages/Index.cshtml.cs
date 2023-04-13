using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin.Pages;

public class IndexModel : PageModel
{
	private LocationService _locationService;

	public IndexModel(LocationService locationService)
	{
		_locationService = locationService;
	}

	public IEnumerable<Location> NewLocations { get; private set; } = Array.Empty<Location>();
	public DateTimeOffset LastUpdate { get; private set; } = default;
	public string? Message { get; private set; } = string.Empty;

	public async Task OnGetAsync()
	{
		NewLocations = (await _locationService.GetNewLocationsAsync(HttpContext.RequestAborted)).OrderByDescending(l => l.Timestamp).ToList();
		LastUpdate = _locationService.LastUpdate ?? DateTimeOffset.UtcNow.Date.AddDays(1).AddTicks(-1);
		Message = Request.Query["m"];
	}
}
