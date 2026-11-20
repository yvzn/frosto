using System;
using System.IO;

internal static class ThisFunctionApp
{
	public static string WorkingDirectory
	{
		get
		{
			// https://stackoverflow.com/questions/68082798/access-functionappdirectory-in-net-5-azure-function
			var local_root = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot");
			var azure_root = $"{Environment.GetEnvironmentVariable("HOME")}/site/wwwroot";
			return local_root ?? azure_root;
		}
	}

	/// <summary>
	/// DKIM / unsubscribe-token private key. DKIM_PRIVATE_KEY_PATH is an optional override
	/// (local runs); unset in production, where dkim_private.pem is deployed next to the app.
	/// </summary>
	public static string DkimPrivateKeyPath
		=> Environment.GetEnvironmentVariable("DKIM_PRIVATE_KEY_PATH") is { Length: > 0 } configured
			? configured
			: Path.Combine(WorkingDirectory, "dkim_private.pem");
}
