using System;

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
}
