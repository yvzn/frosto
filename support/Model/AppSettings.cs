
namespace support.Model;

internal class AppSettings
{
	private static string siteFrUrl = Environment.GetEnvironmentVariable("SITE_FR_URL") ?? throw new Exception("SITE_FR_URL variable not set");
	public static string SiteFrUrl => siteFrUrl;

	private static string siteEnUrl => Environment.GetEnvironmentVariable("SITE_EN_URL") ?? throw new Exception("SITE_EN_URL variable not set");
	public static string SiteEnUrl => siteEnUrl;
}
