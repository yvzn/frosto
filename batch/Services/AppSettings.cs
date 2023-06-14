using System;

namespace batch.Services;

internal class AppSettings
{
	private static string weatherApiUrl = System.Environment.GetEnvironmentVariable("OPEN_METEO_API_URL") ?? throw new Exception("OPEN_METEO_API_URL variable not set");
	public static string WeatherApiUrl => weatherApiUrl;

	private static string smtpUrl = Environment.GetEnvironmentVariable("SMTP_URL") ?? throw new Exception("SMTP_URL variable not set");
	public static string SmtpUrl => smtpUrl;
	private static string smtpLogin = Environment.GetEnvironmentVariable("SMTP_LOGIN") ?? throw new Exception("SMTP_LOGIN variable not set");
	public static string SmtpLogin => smtpLogin;
	private static string smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? throw new Exception("SMTP_PASSWORD variable not set");
	public static string SmtpPassword => smtpPassword;
}
