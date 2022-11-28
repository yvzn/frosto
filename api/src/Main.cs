using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace api;

public class Main
{
	[FunctionName("Main")]
	public async Task RunAsync(
		[TimerTrigger("0 30 6 * * *"
#if DEBUG
			, RunOnStartup=true
#endif
		)]
		TimerInfo timerInfo,
		[Table("validlocation", Connection = "ALERTS_CONNECTION_STRING")]
		TableClient tableClient,
		ILogger log)
	{
		var allLocations = tableClient.QueryAsync<LocationEntity>(filter: e => e.PartitionKey != null);
		await foreach (var location in allLocations)
		{
			log.LogInformation(location.users);
		}
	}
}
