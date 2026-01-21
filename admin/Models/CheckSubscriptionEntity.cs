using Azure;

namespace admin.Models;

public class CheckSubscriptionEntity: EntityBase
{
	public string? email { get; set; }
	public string? userConsent { get; set; }
	public string? lang { get; set; }
}
