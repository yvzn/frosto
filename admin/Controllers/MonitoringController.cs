using admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace admin.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MonitoringController(MonitoringService monitoringService) : ControllerBase
{
	[HttpGet]
	[Route("recent")]
	public async Task<object> GetRecentMonitoringAsync([FromQuery(Name = "days")] int days = 30)
	{
		if (days <= 0) days = 30;
		return await monitoringService.GetRecentMonitoringGeoJSONAsync(days, HttpContext.RequestAborted);
	}
}
