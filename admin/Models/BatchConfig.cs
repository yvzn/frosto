using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace admin.Models;

public class BatchConfig
{
	[Required]
	[DisplayName("Periodicity in days")]
	public int periodInDays { get; set; } = 3;

	[Required]
	[DisplayName("Batches per day")]
	public int batchCountPerDay { get; set; } = 7;
}
