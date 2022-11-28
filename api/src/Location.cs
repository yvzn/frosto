using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace api;

public static class Location
{
#if DEBUG
	[FunctionName("Location")]
	public static IActionResult Run(
		[HttpTrigger(AuthorizationLevel.Function, "post", Route = "location")]
		HttpRequest request,
		[Table("validlocation", Connection = "ALERTS_CONNECTION_STRING")]
		out LocationEntity? locationToSave,
		ILogger log)
	{
		locationToSave = new LocationEntity
		{
			PartitionKey = "France",
			RowKey = Guid.NewGuid().ToString(),
			Timestamp = DateTimeOffset.Now,
			ETag = new(),
			city = "Blain",
			country = "France",
			coordinates = "47.459363,-1.719289",
			users = "alertegelee@outlook.fr"
		};

		return new AcceptedResult();
	}
#endif
}
