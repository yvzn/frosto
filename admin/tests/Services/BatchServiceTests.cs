using admin.Services;

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
}
