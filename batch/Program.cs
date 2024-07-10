using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
	.ConfigureFunctionsWebApplication()
	.ConfigureServices(services => {
		services.AddApplicationInsightsTelemetryWorkerService();
		services.ConfigureFunctionsApplicationInsights();
	})
	.ConfigureLogging(logging =>
	{
		logging.SetMinimumLevel(LogLevel.Warning);
		logging.AddFilter("Function", LogLevel.Warning);
	})
	.Build();

host.Run();
