using admin.Services;

namespace admin.Tests.Services;

public class ChannelServiceTests
{
	[Fact]
	public void GetChannelList_ExcludesLegacyApiChannel()
	{
		var channels = ChannelService.GetChannelList();

		Assert.DoesNotContain("api", channels);
	}
}
