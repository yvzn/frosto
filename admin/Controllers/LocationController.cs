using admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace admin.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LocationController(
	LocationService locationService,
	GeocodingService geocodingService) : ControllerBase
{
	[HttpGet]
	[Route("valid")]
	public async Task<object> GetValidLocationsAsync()
	{
		return await locationService.GetValidLocationsGeoJSONAsync(HttpContext.RequestAborted);
	}

	[HttpGet]
	[Route("")]
	public async Task<IActionResult> GetLocationAsync([FromQuery(Name = "city")] string city, [FromQuery(Name = "country")] string country)
	{
		var location = await locationService.FindValidLocationAsync(city, country, HttpContext.RequestAborted);
		return location is null ? NotFound() : Ok(location);
	}

	[HttpGet]
	[Route("timezone")]
	public IActionResult GetTimezoneAsync([FromQuery(Name = "latitude")] double latitude, [FromQuery(Name = "longitude")] double longitude)
	{
		var timezoneInfo = geocodingService.GetTimezoneInfo(latitude, longitude, HttpContext.RequestAborted);
		if (timezoneInfo is null)
		{
			return NotFound();
		}

		return Ok(timezoneInfo);
	}
}
