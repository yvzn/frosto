using System;
using System.Reflection;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
	.ConfigureFunctionsWebApplication()
	.ConfigureServices(services =>
	{
		services.AddAzureClients(clientBuilder =>
		{
			clientBuilder
				.AddTableServiceClient(Environment.GetEnvironmentVariable("ALERTS_CONNECTION_STRING"));

			string[] tableNames = ["location", "validlocation", "user", "signup"];
			foreach (var tableName in tableNames)
			{
				clientBuilder
					.AddClient<TableClient, TableClientOptions>(
						(_, _, provider) => provider.GetService<TableServiceClient>()!.GetTableClient(tableName))
				.WithName($"{tableName}TableClient");
			}
		});

		services.AddHttpClient("default", client =>
		{
			client.DefaultRequestHeaders.UserAgent.ParseAdd(
				$"Frosto/{Assembly.GetExecutingAssembly().GetName().Version} (https://github.com/yvzn/frosto)");
		});
	})
	.ConfigureLogging(logging =>
	{
		logging.SetMinimumLevel(LogLevel.Warning);
		logging.AddFilter("Function", LogLevel.Warning);
	})
	.Build();

host.Run();
