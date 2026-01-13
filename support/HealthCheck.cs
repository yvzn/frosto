using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace support;

public class HealthCheck
{
	[Function("HealthCheck")]
	public IActionResult Run(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "healthz")]
		HttpRequest _)
	{
		return new OkObjectResult("Healthy");
	}
}
