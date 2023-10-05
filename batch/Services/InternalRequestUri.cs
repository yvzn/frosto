using System;
using System.Collections.Specialized;
using System.Web;

namespace batch.Services;

internal class InternalRequestUri
{
	private readonly string functionName;
	private readonly NameValueCollection queryString;

	public InternalRequestUri(string functionName, NameValueCollection? queryParameters = default)
	{
		this.functionName = functionName;

		queryString = HttpUtility.ParseQueryString(string.Empty);
		queryString.Add("code", AppSettings.InternalApiKey);

		if (queryParameters is not null)
		{
			queryString.Add(queryParameters);
		}

	}

	public string AbsoluteUri
		=> $"{AppSettings.InternalProtocol}://{HostName}/api/{functionName}?{queryString}";

	public static string HostName
		=> Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") ?? string.Empty;
}
