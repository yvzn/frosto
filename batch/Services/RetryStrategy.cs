using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using Polly;

namespace batch.Services;

internal class RetryStrategy
{
	public ResiliencePipeline<HttpResponseMessage> InternalHttp { get; }

	public ResiliencePipeline<HttpResponseMessage> ExternalHttp { get; }

	public ResiliencePipeline DataAccess { get; }

	public ResiliencePipeline<HttpResponseMessage> MailApi { get; }

	public ResiliencePipeline<string?> Smtp { get; }

	// singleton
	public static RetryStrategy For { get; } = new();

	private RetryStrategy()
	{
		var defaultTimeout = TimeSpan.FromSeconds(15);
		var longTimeout = defaultTimeout.Multiply(4);
		var maxRetries = 5;
		static bool IsHttpException(Exception ex) => ex is SocketException or IOException or HttpRequestException;

		InternalHttp = new ResiliencePipelineBuilder<HttpResponseMessage>()
			.AddRetry(new()
			{
				ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
					.Handle<Exception>(IsHttpException)
					.HandleResult(r => !r.IsSuccessStatusCode && r.StatusCode != System.Net.HttpStatusCode.BadRequest && r.StatusCode != System.Net.HttpStatusCode.BadGateway),
				Delay = TimeSpan.FromSeconds(1),
				MaxRetryAttempts = maxRetries,
				BackoffType = DelayBackoffType.Constant
			})
			.AddTimeout(defaultTimeout)
			.Build();

		ExternalHttp = new ResiliencePipelineBuilder<HttpResponseMessage>()
			.AddRetry(new()
			{
				ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
					.Handle<Exception>(IsHttpException)
					.HandleResult(r => !r.IsSuccessStatusCode),
				Delay = TimeSpan.FromSeconds(1),
				MaxRetryAttempts = maxRetries,
				BackoffType = DelayBackoffType.Exponential,
				UseJitter = true,
			})
			.AddTimeout(defaultTimeout)
			.Build();

		DataAccess = new ResiliencePipelineBuilder()
			.AddRetry(new()
			{
				ShouldHandle = new PredicateBuilder()
					.Handle<Azure.RequestFailedException>(),
				Delay = TimeSpan.FromSeconds(1),
				MaxRetryAttempts = maxRetries,
				BackoffType = DelayBackoffType.Constant
			})
			.AddTimeout(defaultTimeout)
			.Build();

		MailApi = new ResiliencePipelineBuilder<HttpResponseMessage>()
			.AddRetry(new()
			{
				ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
					.Handle<Exception>(IsHttpException)
					.HandleResult(r => !r.IsSuccessStatusCode),
				Delay = TimeSpan.FromSeconds(1),
				MaxRetryAttempts = maxRetries,
				BackoffType = DelayBackoffType.Exponential,
				UseJitter = true,
			})
			.Build();

		Smtp = new ResiliencePipelineBuilder<string?>()
			.AddRetry(new()
			{
				ShouldHandle = new PredicateBuilder<string?>()
					.Handle<Exception>(/* any exception */)
					.HandleResult(response => response is null || !response.StartsWith("2.")),
				Delay = TimeSpan.FromSeconds(1),
				MaxRetryAttempts = maxRetries,
				BackoffType = DelayBackoffType.Exponential,
				UseJitter = true,
			})
			.AddTimeout(longTimeout)
			.Build();
	}
}
