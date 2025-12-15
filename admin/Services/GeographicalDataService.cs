namespace admin.Services;

public class GeographicalDataService()
{
	public ICollection<string> GetCountryList()
	{
		return ["France", "Belgique", "Algérie", "Canada", "United kingdom", "United states of america"];
	}

	public ICollection<string> GetCommonTimezones()
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
