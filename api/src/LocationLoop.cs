using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using api.Data;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace api;

public static class LocationLoop
{
	private static Random random = new Random();

	[FunctionName("LocationLoop")]
	public static async Task RunAsync(
		[TimerTrigger("0 0 4 * * *"
#if DEBUG
			, RunOnStartup=true
#endif
		)]
		TimerInfo timerInfo,
		[Table("validlocation", Connection = "ALERTS_CONNECTION_STRING")]
		TableClient tableClient,
		[Queue("location-loop", Connection = "ALERTS_CONNECTION_STRING")]
		QueueClient queueClient,
		ILogger log)
	{
#if DEBUG
		await Task.Delay(5_000);
#endif

		var runningTasks = new List<Task>();
		Expression<Func<LocationEntity, bool>> locationFilter = _ => true;

#if DEBUG
		locationFilter = location => location.uat == true;
#endif

		var validLocations = tableClient.QueryAsync<LocationEntity>(locationFilter);
		var locationIndex = -1;

		await foreach (var location in validLocations)
		{
			++locationIndex;
			var task = ScheduleLocationAsync(location, locationIndex, queueClient, log);
			runningTasks.Add(task);
		}

		await Task.WhenAll(runningTasks);
	}

	private static async Task<bool> ScheduleLocationAsync(LocationEntity location, int locationIndex, QueueClient queueClient, ILogger log)
	{
		var users = location.users?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (users is null || users.Length == 0)
		{
			log.LogWarning("Skipping location {City} {Country} because no user configured", location.city, location.country);
			return false;
		}

		log.LogInformation("Scheduling location {City} {Country} for weather", location.city, location.country);

		try
		{
			var locationId = new EntityKey { PartitionKey = location.PartitionKey, RowKey = location.RowKey };

			var json = JsonSerializer.Serialize(locationId);
			var base64 = EncodeBase64(json);

			var visibilityTimeout = TimeSpan.FromSeconds(90 * locationIndex + random.Next(-30, 30));
			Response<SendReceipt> response = await queueClient.SendMessageAsync(base64, visibilityTimeout);

			if (response.GetRawResponse().IsError)
			{
				log.LogInformation("Scheduling location {City} {Country} for weather", location.city, location.country);

				log.LogError("Failed to schedule location {City} {Country} for weather: {StatusCode} {StatusMessage}", location.city, location.country, response.GetRawResponse().Status, response.GetRawResponse().ReasonPhrase);
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			log.LogError(ex, "Failed to schedule location {City} {Country} for weather", location.city, location.country);
			return false;
		}
	}

	private static string EncodeBase64(string json)
		=> Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
}
