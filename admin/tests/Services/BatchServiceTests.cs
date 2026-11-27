using admin.Models;
using admin.Services;
using System.Text.Json;

namespace admin.Tests.Services;

public class BatchServiceTests
{
	[Fact]
	public void CalculateTargetLocalMinutes_WhenPreferenceIsSet_ReturnsPreference()
	{
		var result = BatchService.CalculateTargetLocalMinutes("France|Paris", 510);

		Assert.Equal(510, result);
	}

	[Fact]
	public void CalculateTargetLocalMinutes_WhenPreferenceIsUnset_UsesHashJitterFallback()
	{
		const string locationId = "France|Paris";
		uint jitterHash = BatchService.StableHash(locationId + ":slot");
		int jitterMinutes = (int)(jitterHash % 120);

		var result = BatchService.CalculateTargetLocalMinutes(locationId, null);

		Assert.Equal(7 * 60 - jitterMinutes, result);
	}

	[Fact]
	public void BatchBuildDiagnostics_SurvivesTempDataSerialization()
	{
		BatchBuildDiagnostics diagnostics = new()
		{
			RebuildAllBatches = true,
			TotalMilliseconds = 1234.5,
			Steps =
			[
				new("Delete existing batches", 100),
				new("Assign locations to batches", 1100),
			],
		};

		var result = JsonSerializer.Deserialize<BatchBuildDiagnostics>(JsonSerializer.Serialize(diagnostics));

		Assert.NotNull(result);
		Assert.True(result.RebuildAllBatches);
		Assert.Equal(diagnostics.TotalMilliseconds, result.TotalMilliseconds);
		Assert.Collection(
			result.Steps,
			step => Assert.Equal("Delete existing batches", step.Name),
			step => Assert.Equal("Assign locations to batches", step.Name));
	}
}
