using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;

namespace api;

public static class Health
{
	[Function("Health")]
	public static IActionResult Run(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "healthz")]
		HttpRequest request)
	{
		return new OkObjectResult("Healthy");
	}
}
