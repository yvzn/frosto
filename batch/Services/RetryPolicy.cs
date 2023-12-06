using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using Polly;

namespace batch.Services;

internal class RetryPolicy
{
	public ResiliencePipeline<HttpResponseMessage> InternalHttpAsync { get; }

	public ResiliencePipeline<HttpResponseMessage> ExternalHttpAsync { get; }

	public ResiliencePipeline DataAccessAsync { get; }

	public ResiliencePipeline<HttpResponseMessage> MailApiAsync { get; }

	public ResiliencePipeline<string?> SmtpAsync { get; }

	public static RetryPolicy For { get; } = new();

	private RetryPolicy()
	{
		var defaultTimoutInSeconds = 15;
		var maxRetries = 5;

		InternalHttpAsync = new ResiliencePipelineBuilder<HttpResponseMessage>()
			.AddRetry(new()
			{
				ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
					.Handle<SocketException>()
					.Handle<HttpRequestException>()
					.HandleResult(r => !r.IsSuccessStatusCode && r.StatusCode != System.Net.HttpStatusCode.BadRequest && r.StatusCode != System.Net.HttpStatusCode.BadGateway),
				Delay = TimeSpan.FromSeconds(1),
				MaxRetryAttempts = maxRetries,
				BackoffType = DelayBackoffType.Constant
			})
			.AddTimeout(TimeSpan.FromSeconds(defaultTimoutInSeconds))
			.Build();

		ExternalHttpAsync = new ResiliencePipelineBuilder<HttpResponseMessage>()
			.AddRetry(new()
			{
				ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
					.Handle<SocketException>()
					.Handle<HttpRequestException>()
					.HandleResult(r => !r.IsSuccessStatusCode),
				Delay = TimeSpan.FromSeconds(1),
				MaxRetryAttempts = maxRetries,
				BackoffType = DelayBackoffType.Exponential,
			})
			.AddTimeout(TimeSpan.FromSeconds(defaultTimoutInSeconds))
			.Build();

		DataAccessAsync = new ResiliencePipelineBuilder()
			.AddRetry(new()
			{
				ShouldHandle = new PredicateBuilder()
					.Handle<Azure.RequestFailedException>(),
				Delay = TimeSpan.FromSeconds(1),
				MaxRetryAttempts = maxRetries,
				BackoffType = DelayBackoffType.Constant
			})
			.AddTimeout(TimeSpan.FromSeconds(defaultTimoutInSeconds))
			.Build();

		MailApiAsync = new ResiliencePipelineBuilder<HttpResponseMessage>()
			.AddRetry(new()
			{
				ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
					.Handle<SocketException>()
					.Handle<HttpRequestException>()
					.HandleResult(r => !r.IsSuccessStatusCode),
				Delay = TimeSpan.FromSeconds(1),
				MaxRetryAttempts = maxRetries,
				BackoffType = DelayBackoffType.Exponential,
			})
			.AddTimeout(TimeSpan.FromSeconds(2 *  defaultTimoutInSeconds))
			.Build();

		SmtpAsync = new ResiliencePipelineBuilder<string?>()
			.AddRetry(new()
			{
				ShouldHandle = new PredicateBuilder<string?>()
					.Handle<Exception>()
					.HandleResult(response => response is null || !response.StartsWith("2.")),
				Delay = TimeSpan.FromSeconds(1),
				MaxRetryAttempts = maxRetries,
				BackoffType = DelayBackoffType.Exponential,
			})
			.AddTimeout(TimeSpan.FromSeconds(defaultTimoutInSeconds))
			.Build();
	}
}
