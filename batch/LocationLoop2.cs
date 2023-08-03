using System;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using batch.Models;
using batch.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace batch;

public class LocationLoop2
{
	private static Random random = new();

	private static HttpClient httpClient = new();

	[FunctionName("LocationLoop2")]
	public static async Task RunAsync(
		[TimerTrigger("0 0 4 * 1-5,10-12 *"
#if true //DEBUG
			, RunOnStartup=true
#endif
		)]
		TimerInfo timerInfo,
		[Table("validlocation", Connection = "ALERTS_CONNECTION_STRING")]
		TableClient tableClient,
		ILogger log)
	{
#if DEBUG
		await Task.Delay(5_000);
#endif

		Expression<Func<LocationEntity, bool>> locationFilter = _ => true;

#if true //DEBUG
		locationFilter = location => location.uat == true;
#endif

		var query = () => tableClient.QueryAsync<LocationEntity>(locationFilter);
		var validLocations = RetryPolicy.ForDataAccess.Execute(query);

		int locationIndex = -1;
		await foreach (var location in validLocations)
		{
			Interlocked.Increment(ref locationIndex);
			var _ = ScheduleLocationAsync(location, locationIndex, log);
		}
	}

	private static async Task<bool> ScheduleLocationAsync(LocationEntity location, int locationIndex, ILogger log)
	{
		var users = location.users?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (users is null || users.Length == 0)
		{
			log.LogWarning("Skipping location {City} {Country} because no user configured", location.city, location.country);
			return false;
		}

		log.LogInformation("Scheduling location {City} {Country} for weather", location.city, location.country);

		var requestUri = $"http://{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}/api/NotifyAtLocation2?p={location.PartitionKey}&r={location.RowKey}&code={AppSettings.InternalApiKey}";

		var response = default(HttpResponseMessage);

		try
		{
			var visibilityTimeout = TimeSpan.FromMilliseconds(1_000 * locationIndex + random.Next(500));
			await Task.Delay(visibilityTimeout, CancellationToken.None);

			var request = () => httpClient.GetAsync(requestUri);
			response = await RetryPolicy.ForInternalHttpAsync.ExecuteAsync(request);

			if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.BadGateway)
			{
				log.LogError("Failed to schedule location {City} {Country} for weather: HTTP {StatusCode} {RequestUri}", location.city, location.country, response.StatusCode, requestUri);
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			log.LogError(ex, "Failed to schedule location {City} {Country} for weather: HTTP {StatusCode} {RequestUri}", location.city, location.country, response?.StatusCode, requestUri);
			return false;
		}
	}
}
