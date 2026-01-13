using Azure.Data.Tables;
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
	.AddAzureClients(clientBuilder =>
		{
			clientBuilder
				.AddTableServiceClient(Environment.GetEnvironmentVariable("ALERTS_CONNECTION_STRING"));

			string[] tableNames = ["checksubscription"];
			foreach (var tableName in tableNames)
			{
				clientBuilder
					.AddClient<TableClient, TableClientOptions>(
						(_, _, provider) => provider.GetService<TableServiceClient>()!.GetTableClient(tableName))
				.WithName($"{tableName}TableClient");
			}
		});

builder.Build().Run();
