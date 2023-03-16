using System;

internal class AppSettings
{
	private static string weatherApiUrl = System.Environment.GetEnvironmentVariable("WEATHER_API_URL") ?? throw new Exception("WEATHER_API_URL variable not set");
	public static string WeatherApiUrl => weatherApiUrl;

	private static string sendMailApiUrl = Environment.GetEnvironmentVariable("SEND_MAIL_API_URL") ?? throw new Exception("SEND_MAIL_API_URL variable not set");
	public static string SendMailApiUrl => sendMailApiUrl;

	private static string tipiMailApiUrl = Environment.GetEnvironmentVariable("TIPI_MAIL_API_URL") ?? throw new Exception("TIPI_MAIL_API_URL variable not set");
	public static string TipiMailApiUrl => tipiMailApiUrl;

	private static string tipiMailApiUser = Environment.GetEnvironmentVariable("TIPI_MAIL_API_USER") ?? throw new Exception("TIPI_MAIL_API_USER variable not set");
	public static string TipiMailApiUser => tipiMailApiUser;

	private static string tipiMailApiKey = Environment.GetEnvironmentVariable("TIPI_MAIL_API_KEY") ?? throw new Exception("TIPI_MAIL_API_KEY variable not set");
	public static string TipiMailApiKey => tipiMailApiKey;

	private static string alertsConnectionString = Environment.GetEnvironmentVariable("ALERTS_CONNECTION_STRING") ?? throw new Exception("ALERTS_CONNECTION_STRING variable not set");
	public static string AlertsConnectionString => alertsConnectionString;
}
