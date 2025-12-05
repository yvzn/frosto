namespace admin.Models;

public class SignUp : ModelBase
{

	public string city { get; set; } = "";
	public string country { get; set; } = "";
	public string lang { get; set; } = "";

	public string Id
	{
		get => GetId();
		set => SetId(value);
	}
}
