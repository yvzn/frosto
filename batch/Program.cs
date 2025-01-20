using Azure.Data.Tables;
using batch.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
	.ConfigureFunctionsWebApplication()
	.ConfigureServices(services => {
		services.AddHttpClient();

		services.AddApplicationInsightsTelemetryWorkerService();
		services.ConfigureFunctionsApplicationInsights();

		services.AddAzureClients(clientBuilder =>
		{
			clientBuilder.AddTableServiceClient(AppSettings.AlertsConnectionString);

			string[] tableNames = ["validlocation", "batch"];
			foreach (var tableName in tableNames)
			{
				clientBuilder
					.AddClient<TableClient, TableClientOptions>(
						(_, _, provider) => provider.GetService<TableServiceClient>()!.GetTableClient(tableName))
				.WithName($"{tableName}TableClient");
			}
		});
	})
	.ConfigureLogging(logging =>
	{
		logging.SetMinimumLevel(LogLevel.Warning);
		logging.AddFilter("Function", LogLevel.Warning);
		logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
	})
	.Build();

host.Run();
