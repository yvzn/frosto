namespace admin.Models;

public class Feedback : ModelBase
{
	public string Sentiment { get; set; } = "";
	public string Text { get; set; } = "";
	public string Email { get; set; } = "";
	public bool Consent { get; set; }
	public string Language { get; set; } = "";
	public string Source { get; set; } = "";
	public string LocationPartitionKey { get; set; } = "";
	public string LocationRowKey { get; set; } = "";

	public string Id
	{
		get => GetId();
		set => SetId(value);
	}
}
