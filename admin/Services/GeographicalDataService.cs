namespace admin.Services;

public class GeographicalDataService()
{
	public ICollection<string> GetCountryList()
	{
		return ["France", "Belgique", "Algérie", "Canada", "United kingdom", "United states of america"];
	}
}
