using System;

namespace api.Data;

internal class AppSettings
{
	private static string siteUrl = Environment.GetEnvironmentVariable("SITE_URL") ?? throw new Exception("SITE_URL variable not set");
	public static string SiteUrl => siteUrl;
}
