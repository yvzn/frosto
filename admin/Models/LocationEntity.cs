
namespace admin.Models;

public class LocationEntity : EntityBase
{
	public string? city { get; set; }
	public string? country { get; set; }
	public string? coordinates { get; set; }
	public string? users { get; set; }
	public bool? uat { get; set; }
	public string? disabled { get; set; }
	public string? channel { get; set; }
	public string? zipCode { get; set; }
	public string? lang { get; set; }
	public string? timezone { get; set; }
	public string? offset { get; set; }
}
