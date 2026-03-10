namespace admin.Services;

public class ChannelService()
{
	public static string DefaultChannel => "scaleway";

	public static string[] GetChannelList()
	{
		return ["", "api", "smtp", "default", "tipimail", "scaleway"];
	}
}
