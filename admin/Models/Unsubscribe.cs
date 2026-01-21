namespace admin.Models;

public class Unsubscribe : ModelBase
{
	public string token { get; set; } = "";
	public string lang { get; set; } = "";

	public string Id
	{
		get => GetId();
		set => SetId(value);
	}
}
