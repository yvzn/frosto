using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
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

public static class Main
{
#if DEBUG
	private static readonly decimal threshold = 5.0m;
#else
	private static readonly decimal threshold = 1.0m;
#endif

	private static HttpClient httpClient = new();

	private static string weatherApiUrl = System.Environment.GetEnvironmentVariable("WEATHER_API_URL") ?? throw new Exception("WEATHER_API_URL variable not set");

	[FunctionName("Main")]
	public static async Task RunAsync(
		[TimerTrigger("0 30 6 * * *"
#if DEBUG
			, RunOnStartup=true
#endif
		)]
		TimerInfo timerInfo,
		[Table("validlocation", Connection = "ALERTS_CONNECTION_STRING")]
		TableClient tableClient,
		[Queue("emailoutbox", Connection = "ALERTS_CONNECTION_STRING")]
		QueueClient queueClient,
		ILogger log)
	{
#if DEBUG
		await Task.Delay(5_000);
#endif
		var runningTasks = new List<Task>();
		var validLocations = tableClient.QueryAsync<LocationEntity>(filter: location => location.uat == true);

		await foreach (var location in validLocations)
		{
			var task = NotifyIfFrostAsync(location, queueClient, log);
			runningTasks.Add(task);
		}

		await Task.WhenAll(runningTasks);
	}

	private static async Task NotifyIfFrostAsync(LocationEntity location, QueueClient queueClient, ILogger log)
	{
		var forecasts = await GetWeatherForecastsAsync(location.coordinates, log);

		var forecastsBelowThreshold = forecasts?.Where(f => f.Minimum <= threshold).ToList();
		if (forecastsBelowThreshold?.Any() is true)
		{
			var subject = FormatNotificationSubject(forecastsBelowThreshold);
			var body = FormatNotificationBody(forecastsBelowThreshold, location);
			await ScheduleNotificationAsync(subject, body, location.users, queueClient, log);
		}
	}

	private static async Task<IList<Forecast>?> GetWeatherForecastsAsync(string? coordinates, ILogger log)
	{
		log.LogInformation("Get weather forecast for {Coordinates}", coordinates);

		if (coordinates is null) throw new ArgumentNullException(nameof(coordinates));

		try
		{
			var response = await httpClient.GetAsync(string.Format(weatherApiUrl, coordinates));

			if (!response.IsSuccessStatusCode)
			{
				log.LogError("Failed to get weather forecast for {Coordinates}: {StatusCode} {StatusMessage}", coordinates, response.StatusCode, await response.Content.ReadAsStringAsync());
				return null;
			}

			var weatherApiResult = await JsonSerializer.DeserializeAsync<WeatherApiResult>(
				await response.Content.ReadAsStreamAsync());

			return weatherApiResult?.forecasts
				.Where(forecast => forecast.date is not null
						&& forecast.temperature?.minimum?.value.HasValue is true
						&& forecast.temperature?.minimum?.value.HasValue is true)
				.Select(forecast => new Forecast(
					DateTime.Parse(forecast.date ?? ""),
					forecast.temperature!.minimum!.value!.Value,
					forecast.temperature!.maximum!.value!.Value))
				.ToList();
		}
		catch (Exception ex)
		{
			log.LogError(ex, "Failed to get weather forecast for {Coordinates}", coordinates);
			return null;
		}
	}

	private static readonly CultureInfo cultureInfo = CultureInfo.CreateSpecificCulture("fr-FR");

	private static string FormatNotificationSubject(List<Forecast> forecasts)
	{
		var header = "Températures proches de zéro prévues ces prochains jours";
		var forecastsBelow0 = forecasts.Where(f => f.Minimum < 0).ToList();
		if (forecastsBelow0.Any())
		{
			var first = forecastsBelow0.OrderBy(f => f.Date).First();
			header = string.Format(
				cultureInfo,
				"Températures négatives prévues {0:dddd d MMMM}: {1}°",
				first.Date,
				first.Minimum
			);
		}

		return header;
	}

	private static readonly string tableHeaderTemplate = "<table><thead><tr><th>date<th>minimum<th>maximum</thead><tbody>";

	private static readonly string tableRowTemplate = "<tr><td>{0:dddd d MMMM}<td>{1}° {2}<td>{3}°</tr>";

	private static readonly string tableFooterTemplate = "</tbody></table>";

	private static readonly string notificationTemplate =
@"<p>Bonjour,

<p>Les prévisions de température des prochains jours ({0}, {1}):

{2}

<p>Cordialement,
<br>L'équipe Alertegelee.fr

<p>Pour vous désinscrire, répondez ""STOP"" à ce message.";

	private static string FormatNotificationBody(List<Forecast> forecasts, LocationEntity location)
	{
		var table = new StringBuilder();
		table.Append(tableHeaderTemplate);
		table.Append(Environment.NewLine);

		foreach (var forecast in forecasts.OrderBy(f => f.Date))
		{
			table.Append(string.Format(
				cultureInfo,
				tableRowTemplate,
				forecast.Date,
				forecast.Minimum,
				forecast.Minimum < 0 ? '❄' : ' ',
				forecast.Maximum
			));
			table.Append(Environment.NewLine);
		}

		table.Append(tableFooterTemplate);

		return string.Format(
				cultureInfo,
				notificationTemplate,
				location.city,
				location.country,
				table.ToString()
			);
	}

	private static async Task<bool> ScheduleNotificationAsync(string subject, string body, string? users, QueueClient queueClient, ILogger log)
	{
		log.LogInformation("Scheduling notification to {Users}", users);

		if (users is null) throw new ArgumentNullException(nameof(users));

		try
		{
			var sendMailRequest = new SendMailRequest
			{
				subject = subject,
				body = body,
				to = users.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			};

			var json = JsonSerializer.Serialize(sendMailRequest);
			var bytes = Encoding.UTF8.GetBytes(json);
			var base64 = Convert.ToBase64String(bytes);

			Response<SendReceipt> response = await queueClient.SendMessageAsync(base64);

			if (response.GetRawResponse().IsError)
			{
				log.LogError("Failed to schedule notification to {Users}: {StatusCode} {StatusMessage}", users, response.GetRawResponse().Status, response.GetRawResponse().ReasonPhrase);
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			log.LogError(ex, "Failed to schedule notification to {Users}", users);
			return false;
		}
	}
}
