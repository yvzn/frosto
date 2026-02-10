namespace admin.Models;

public class UnsubscribeEntity: EntityBase
{
	public string? user { get; set; }
	public string? email { get; set; }
	public string? locid { get; set; }
	public string? reason { get; set; }
	public string? origin { get; set; }
	public string? lang { get; set; }
}
