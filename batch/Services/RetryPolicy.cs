using System;
using System.Net.Http;
using System.Net.Sockets;
using Polly;
using Polly.Registry;

namespace batch.Services;

internal static class RetryPolicy
{
	public static IAsyncPolicy<HttpResponseMessage> ForInternalHttpAsync => (IAsyncPolicy<HttpResponseMessage>)policyRegistry["ForInternalHttpAsync"];

	public static IAsyncPolicy<HttpResponseMessage> ForExternalHttpAsync => (IAsyncPolicy<HttpResponseMessage>)policyRegistry["ForExternalHttpAsync"];

	public static ISyncPolicy ForDataAccess => (ISyncPolicy)policyRegistry["ForDataAccess"];

	public static IAsyncPolicy ForDataAccessAsync => (IAsyncPolicy)policyRegistry["ForDataAccessAsync"];

	public static IAsyncPolicy<string?> ForSmtpAsync => (IAsyncPolicy<string?>)policyRegistry["ForSmtpAsync"];

	private static readonly PolicyRegistry policyRegistry = BuildPolicyRegistry();

	private static PolicyRegistry BuildPolicyRegistry()
	{
		var registry = new PolicyRegistry();

		var defaultTimoutInSeconds = 15;

		var defaultRetry = new[] {
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(1)
		};

		var exponentialBackoff = new[] {
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(2),
			TimeSpan.FromSeconds(4)
#if !DEBUG
			, TimeSpan.FromSeconds(8)
			, TimeSpan.FromSeconds(16)
#endif
		};

		registry["ForInternalHttpAsync"] = Policy.WrapAsync(
			new IAsyncPolicy<HttpResponseMessage>[] {
				Policy
					.Handle<SocketException>()
					.Or<HttpRequestException>()
					.OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && r.StatusCode != System.Net.HttpStatusCode.BadRequest && r.StatusCode != System.Net.HttpStatusCode.BadGateway)
					.WaitAndRetryAsync(defaultRetry),
				Policy
					.TimeoutAsync<HttpResponseMessage>(defaultTimoutInSeconds)
			});

		registry["ForExternalHttpAsync"] = Policy.WrapAsync(
			new IAsyncPolicy<HttpResponseMessage>[] {
				Policy
					.Handle<SocketException>()
					.Or<HttpRequestException>()
					.OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
					.WaitAndRetryAsync(exponentialBackoff),
				Policy
					.TimeoutAsync<HttpResponseMessage>(defaultTimoutInSeconds)
			});

		registry["ForDataAccess"] = Policy.Wrap(
			new ISyncPolicy[] {
				Policy
					.Handle<Azure.RequestFailedException>()
					.WaitAndRetry(defaultRetry),
				Policy
					.Timeout(defaultTimoutInSeconds)
			});

		registry["ForDataAccessAsync"] = Policy.WrapAsync(
			new IAsyncPolicy[] {
				Policy
					.Handle<Azure.RequestFailedException>()
					.WaitAndRetryAsync(defaultRetry),
				Policy
					.TimeoutAsync(defaultTimoutInSeconds)
			});

		registry["ForSmtpAsync"] = Policy.WrapAsync(
			new IAsyncPolicy<string?>[] {
				Policy
					.Handle<Exception>()
					.OrResult<string?>(response => response is null || !response.StartsWith("2."))
					.WaitAndRetryAsync(exponentialBackoff),
				Policy
					.TimeoutAsync<string?>(defaultTimoutInSeconds)
			});

		return registry;
	}
}
