using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Frosto.Entities;

public static class TableServiceClientFactory
{
	public static IServiceCollection AddAlertsTableServiceClient(
		this IServiceCollection services,
		string? tableEndpoint,
		string? connectionString)
	{
		services.AddSingleton(_ => Create(tableEndpoint, connectionString));
		return services;
	}

	public static TableServiceClient Create(string? tableEndpoint, string? connectionString)
	{
		if (!string.IsNullOrWhiteSpace(tableEndpoint))
		{
			return new TableServiceClient(new Uri(tableEndpoint), new DefaultAzureCredential());
		}

		if (string.IsNullOrWhiteSpace(connectionString))
		{
			throw new InvalidOperationException("Table storage endpoint or connection string must be configured.");
		}

		return new TableServiceClient(connectionString);
	}
}
