using System;
using System.Collections.Immutable;
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
		var defaultTimeout = TimeSpan.FromSeconds(15);
		var maxRetries = 5;
		ImmutableArray<Type> httpExceptions = new[] { typeof(SocketException), typeof(IOException), typeof(HttpRequestException) }.ToImmutableArray();

		InternalHttpAsync = new ResiliencePipelineBuilder<HttpResponseMessage>()
			.AddRetry(new()
			{
				ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
					.Handle<Exception>(ex => httpExceptions.Contains(ex.GetType()))
					.HandleResult(r => !r.IsSuccessStatusCode && r.StatusCode != System.Net.HttpStatusCode.BadRequest && r.StatusCode != System.Net.HttpStatusCode.BadGateway),
				Delay = TimeSpan.FromSeconds(1),
				MaxRetryAttempts = maxRetries,
				BackoffType = DelayBackoffType.Constant
			})
			.AddTimeout(defaultTimeout)
			.Build();

		ExternalHttpAsync = new ResiliencePipelineBuilder<HttpResponseMessage>()
			.AddRetry(new()
			{
				ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
					.Handle<Exception>(ex => httpExceptions.Contains(ex.GetType()))
					.HandleResult(r => !r.IsSuccessStatusCode),
				Delay = TimeSpan.FromSeconds(1),
				MaxRetryAttempts = maxRetries,
				BackoffType = DelayBackoffType.Exponential,
				UseJitter = true,
			})
			.AddTimeout(defaultTimeout)
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
			.AddTimeout(defaultTimeout)
			.Build();

		MailApiAsync = new ResiliencePipelineBuilder<HttpResponseMessage>()
			.AddRetry(new()
			{
				ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
					.Handle<Exception>(ex => httpExceptions.Contains(ex.GetType()))
					.HandleResult(r => !r.IsSuccessStatusCode),
				Delay = TimeSpan.FromSeconds(1),
				MaxRetryAttempts = maxRetries,
				BackoffType = DelayBackoffType.Exponential,
				UseJitter = true,
			})
			.AddTimeout(defaultTimeout.Multiply(2))
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
				UseJitter = true,
			})
			.AddTimeout(defaultTimeout)
			.Build();
	}
}
