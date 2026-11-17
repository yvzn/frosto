using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace admin.Models;

public class BatchConfig
{
	[Required]
	[DisplayName("Cycle length in days (N)")]
	public int periodInDays { get; set; } = 2;

	[Required]
	[DisplayName("Capacity guard multiplier")]
	public double capacityGuardMultiplier { get; set; } = 3;
}
