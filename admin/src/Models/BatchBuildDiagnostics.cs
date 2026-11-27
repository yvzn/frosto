namespace admin.Models;

public sealed class BatchBuildDiagnostics
{
	public bool RebuildAllBatches { get; init; }
	public List<BatchBuildStepTiming> Steps { get; init; } = [];
	public double TotalMilliseconds { get; init; }
}

public sealed record BatchBuildStepTiming(string Name, double ElapsedMilliseconds);
