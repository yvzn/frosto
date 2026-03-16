using System;

namespace api.Data;

internal class AppSettings
{
	private static readonly string siteUrl = TryGetEnvironmentVariable("SITE_URL");
	public static string SiteUrl => siteUrl;

	private static readonly string siteEnUrl = TryGetEnvironmentVariable("SITE_EN_URL");
	public static string SiteEnUrl => siteEnUrl;

	private static readonly string weatherApiUrl = TryGetEnvironmentVariable("OPEN_METEO_API_URL");
	public static string WeatherApiUrl => weatherApiUrl;

	private static string TryGetEnvironmentVariable(string variable)
		=> Environment.GetEnvironmentVariable(variable) ?? throw new Exception($"{variable} variable not set");
}
