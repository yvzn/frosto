using Azure;
using Azure.Data.Tables;

namespace admin.Models;

public class MonitoringEntity : EntityBase
{
	public string? coordinates { get; set; }
	public int? daysUntilNextFrost { get; set; }
}
