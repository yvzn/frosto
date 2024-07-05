using admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace admin.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LocationController : ControllerBase
{
	private LocationService _locationService;

	public LocationController(LocationService locationService)
	{
		_locationService = locationService;
	}

	[HttpGet]
	[Route("valid")]
	public async Task<object> GetValidLocationsAsync()
	{
		return await _locationService.GetValidLocationsGeoJSONAsync(HttpContext.RequestAborted);
	}

	[HttpGet]
	[Route("")]
	public async Task<IActionResult> GetLocationAsync([FromQuery(Name = "city")] string city, [FromQuery(Name = "country")] string country)
	{
		var location = await _locationService.FindValidLocationAsync(city, country, HttpContext.RequestAborted);
		return location is null ? NotFound() : Ok(location);
	}
}
