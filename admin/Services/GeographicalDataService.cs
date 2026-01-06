namespace admin.Services;

public class GeographicalDataService()
{
	public static ICollection<string> GetCountryList()
	{
		return ["France", "Algérie", "Belgique", "Canada", "Deutschland", "United kingdom", "United states of america"];
	}

	public static ICollection<string> GetCommonTimezones()
	{
		return [
			"Europe/Brussels",
			"Africa/Algiers",
			"America/Toronto",
			"America/Vancouver",
			"Europe/London",
			"America/New_York",
			"America/Chicago",
			"America/Denver",
			"America/Los_Angeles"
		];
	}
}
