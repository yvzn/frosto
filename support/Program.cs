using Azure.Data.Tables;
using Frosto.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
	.AddApplicationInsightsTelemetryWorkerService()
	.ConfigureFunctionsApplicationInsights()
	.AddAlertsTableServiceClient(
		Environment.GetEnvironmentVariable("ALERTS_TABLE_ENDPOINT"),
		Environment.GetEnvironmentVariable("ALERTS_CONNECTION_STRING"))
	.AddAzureClients(clientBuilder =>
		{
			string[] tableNames = ["checksubscription", "unsubscribe", "feedback"];
			foreach (var tableName in tableNames)
			{
				clientBuilder
					.AddClient<TableClient, TableClientOptions>(
						(_, _, provider) => provider.GetService<TableServiceClient>()!.GetTableClient(tableName))
				.WithName($"{tableName}TableClient");
			}
		});

builder.Build().Run();
