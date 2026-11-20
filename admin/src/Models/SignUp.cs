namespace admin.Models;

public class SignUp : ModelBase
{

	public string city { get; set; } = "";
	public string country { get; set; } = "";
	public string lang { get; set; } = "";
	public string? temperatureUnit { get; set; }
	public string? comment { get; set; }
	public int? duration { get; set; }

	public IReadOnlyList<string> ReviewNotes { get; set; } = [];

	public string Id
	{
		get => GetId();
		set => SetId(value);
	}
}
