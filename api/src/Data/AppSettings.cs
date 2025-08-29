using System;

namespace api.Data;

internal class AppSettings
{
	private static string siteUrl = Environment.GetEnvironmentVariable("SITE_URL") ?? throw new Exception("SITE_URL variable not set");
	public static string SiteUrl => siteUrl;

	private static string siteEnUrl => Environment.GetEnvironmentVariable("SITE_EN_URL") ?? throw new Exception("SITE_EN_URL variable not set");
	public static string SiteEnUrl => siteEnUrl;
}
