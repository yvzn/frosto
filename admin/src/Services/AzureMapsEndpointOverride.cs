using Azure.Core;
using Azure.Core.Pipeline;

namespace admin.Services;

/// <summary>
/// Optional: sends the Azure Maps SDK requests to another host (a local mock) when the
/// "AzureMapsEndpoint" setting is present. Without it nothing changes (real Azure Maps).
/// </summary>
internal static class AzureMapsEndpointOverride
{
	public static T Apply<T>(T options, string? endpoint) where T : ClientOptions
	{
		if (!string.IsNullOrWhiteSpace(endpoint))
		{
			options.AddPolicy(new RewriteHostPolicy(new Uri(endpoint)), HttpPipelinePosition.PerRetry);
		}

		return options;
	}

	private sealed class RewriteHostPolicy(Uri target) : HttpPipelineSynchronousPolicy
	{
		public override void OnSendingRequest(HttpMessage message)
		{
			message.Request.Uri.Scheme = target.Scheme;
			message.Request.Uri.Host = target.Host;
			message.Request.Uri.Port = target.Port;
		}
	}
}
