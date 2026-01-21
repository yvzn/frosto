namespace admin.Models;

public class CheckSubscription : ModelBase
{
	public string email { get; set; } = "";
	public string userConsent { get; set; } = "";
	public string lang { get; set; } = "";

	public string Id
	{
		get => GetId();
		set => SetId(value);
	}
}
