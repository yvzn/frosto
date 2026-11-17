namespace admin.Models;

public class BatchStats
{
	public int Total { get; set; }
	public int[] SlotCounts { get; set; } = new int[72];
	public Dictionary<int, int> DayCounts { get; set; } = new();
	public int InTimeWindow { get; set; }
	public int MissingOffset { get; set; }
	public List<OutOfWindowLocation> OutOfWindow { get; set; } = new();
	public List<BatchSampleLocation> RandomSamples { get; set; } = new();

	public double SlotMin => SlotCounts.Length > 0 ? SlotCounts.Min() : 0;
	public double SlotMax => SlotCounts.Length > 0 ? SlotCounts.Max() : 0;
	public double SlotAvg => Total / 72.0;

	public double SlotStdDev
	{
		get
		{
			if (SlotCounts.Length == 0) return 0;
			double avg = SlotAvg;
			double variance = SlotCounts.Average(c => (c - avg) * (c - avg));
			return Math.Sqrt(variance);
		}
	}

	public double InWindowPercent => Total > 0 ? 100.0 * InTimeWindow / Total : 0;
}

public record OutOfWindowLocation(string City, string Country, int SlotIndex, int LocalMinutes);

public record BatchSampleLocation(string City, string Country, string Hemisphere, int DayIndex, int SlotIndex, int UtcOffsetMinutes);
