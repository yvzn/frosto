using System;

namespace batch.Services;

internal static class AppSettings
{
	private static string weatherApiUrl = System.Environment.GetEnvironmentVariable("OPEN_METEO_API_URL") ?? throw new Exception("OPEN_METEO_API_URL variable not set");
	public static string WeatherApiUrl => weatherApiUrl;

	private static string sendMailApiUrl = Environment.GetEnvironmentVariable("SEND_MAIL_API_URL") ?? throw new Exception("SEND_MAIL_API_URL variable not set");
	public static string SendMailApiUrl => sendMailApiUrl;

	private static string tipiMailApiUrl = Environment.GetEnvironmentVariable("TIPI_MAIL_API_URL") ?? throw new Exception("TIPI_MAIL_API_URL variable not set");
	public static string TipiMailApiUrl => tipiMailApiUrl;

	private static string tipiMailApiUser = Environment.GetEnvironmentVariable("TIPI_MAIL_API_USER") ?? throw new Exception("TIPI_MAIL_API_USER variable not set");
	public static string TipiMailApiUser => tipiMailApiUser;

	private static string tipiMailApiKey = Environment.GetEnvironmentVariable("TIPI_MAIL_API_KEY") ?? throw new Exception("TIPI_MAIL_API_KEY variable not set");
	public static string TipiMailApiKey => tipiMailApiKey;

	private static string smtpUrl = Environment.GetEnvironmentVariable("SMTP_URL") ?? throw new Exception("SMTP_URL variable not set");
	public static string SmtpUrl => smtpUrl;
	private static string smtpLogin = Environment.GetEnvironmentVariable("SMTP_LOGIN") ?? throw new Exception("SMTP_LOGIN variable not set");
	public static string SmtpLogin => smtpLogin;
	private static string smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? throw new Exception("SMTP_PASSWORD variable not set");
	public static string SmtpPassword => smtpPassword;

	private static string internalApiKey = Environment.GetEnvironmentVariable("INTERNAL_API_KEY") ?? throw new Exception("INTERNAL_API_KEY variable not set");
	public static string InternalApiKey => internalApiKey;
}
