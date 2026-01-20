using System.Reflection;
using Azure.Data.Tables;
using batch.Services;
using batch.Services.SendMail;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
	.ConfigureFunctionsWebApplication()
	.ConfigureServices(services => {
		services.AddHttpClient("default", client =>
		{
			client.DefaultRequestHeaders.UserAgent.ParseAdd(
				$"Frosto/{Assembly.GetExecutingAssembly().GetName().Version} (https://github.com/yvzn/frosto)");
		});

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

		services.AddKeyedScoped<IMailSender, TipiMailSender>("tipimail");
		services.AddKeyedScoped<IMailSender, SmtpMailSender>("smtp");
		services.AddKeyedScoped<IMailSender, ApiMailSender>("api");
		services.AddKeyedScoped<IMailSender, ScalewayMailSender>("scaleway");
		services.AddKeyedScoped<IMailSender, ApiMailSender>("default");
	})
	.ConfigureLogging(logging =>
	{
		logging.SetMinimumLevel(LogLevel.Warning);
		logging.AddFilter("Function", LogLevel.Warning);
		logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
	})
	.Build();

host.Run();
