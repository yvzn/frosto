using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;

namespace api;

public static class Health
{
	[FunctionName("Health")]
	public static IActionResult Run(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "healthz")]
		HttpRequest request)
	{
		return new OkObjectResult("Healthy");
	}
}
