using System;

namespace batch.Services;

internal static class AppSettings
{
	private static readonly string weatherApiUrl = TryGetEnvironmentVariable("OPEN_METEO_API_URL");
	public static string WeatherApiUrl => weatherApiUrl;

	private static readonly string sendMailApiUrl = TryGetEnvironmentVariable("SEND_MAIL_API_URL");
	public static string SendMailApiUrl => sendMailApiUrl;

	private static readonly string tipiMailApiUrl = TryGetEnvironmentVariable("TIPI_MAIL_API_URL");
	public static string TipiMailApiUrl => tipiMailApiUrl;

	private static readonly string tipiMailApiUser = TryGetEnvironmentVariable("TIPI_MAIL_API_USER");
	public static string TipiMailApiUser => tipiMailApiUser;

	private static readonly string tipiMailApiKey = TryGetEnvironmentVariable("TIPI_MAIL_API_KEY");
	public static string TipiMailApiKey => tipiMailApiKey;

	private static readonly string smtpUrl = TryGetEnvironmentVariable("SMTP_URL");
	public static string SmtpUrl => smtpUrl;
	private static readonly string smtpLogin = TryGetEnvironmentVariable("SMTP_LOGIN");
	public static string SmtpLogin => smtpLogin;
	private static readonly string smtpPassword = TryGetEnvironmentVariable("SMTP_PASSWORD");
	public static string SmtpPassword => smtpPassword;

	private static readonly string internalApiKey = TryGetEnvironmentVariable("INTERNAL_API_KEY");
	public static string InternalApiKey => internalApiKey;

	private static readonly string internalProtocol = TryGetEnvironmentVariable("INTERNAL_PROTOCOL");
	public static string InternalProtocol => internalProtocol;

	private static readonly string alertsConnectionString = TryGetEnvironmentVariable("ALERTS_CONNECTION_STRING");
	public static string AlertsConnectionString => alertsConnectionString;

	private static readonly int periodInDays = int.Parse(TryGetEnvironmentVariable("PERIOD_IN_DAYS"));
	public static int PeriodInDays => periodInDays;

	private static string TryGetEnvironmentVariable(string variable)
		=> Environment.GetEnvironmentVariable(variable) ?? throw new Exception($"{variable} variable not set");
}
