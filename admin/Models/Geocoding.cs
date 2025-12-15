namespace admin.Models;

public class Geocoding
{
	public string Locality { get; set; } = string.Empty;
	public string Country { get; set; } = string.Empty;
	public double Latitude { get; set; }
	public double Longitude { get; set; }
}
