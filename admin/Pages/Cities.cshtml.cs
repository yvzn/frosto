using admin.Models;
using admin.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admin;

public class CitiesModel : PageModel
{
	private readonly LocationService _locationService;

	public CitiesModel(LocationService locationService)
	{
		_locationService = locationService;
	}

	public IEnumerable<Location> ValidLocations { get; private set; } = Array.Empty<Location>();

	public async Task OnGetAsync()
	{
		ValidLocations = (await _locationService.GetValidLocationsAsync(HttpContext.RequestAborted)).OrderBy(l => l.city).ToList();
	}
}
