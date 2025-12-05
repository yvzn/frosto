using admin.Services;
using Azure.Data.Tables;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

string[] azureStorageTableNames = ["location", "validlocation", "batch", "signup", "user"];

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder
        .AddTableServiceClient(builder.Configuration.GetConnectionString("Alerts"));

    foreach (var tableName in azureStorageTableNames)
    {
        clientBuilder
            .AddClient<TableClient, TableClientOptions>(
                (_, _, provider) => provider.GetService<TableServiceClient>()!.GetTableClient(tableName))
        .WithName($"{tableName}TableClient");
    }
});

builder.Services.AddScoped<LocationService>();
builder.Services.AddScoped<SignUpService>();
builder.Services.AddScoped<BatchService>();
builder.Services.AddScoped<GeographicalDataService>();
builder.Services.AddScoped<UnsubscribeService>();
builder.Services.AddScoped<UserService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler("/Error");

app.MapStaticAssets();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

#if DEBUG
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var tableServiceClient = services.GetService<TableServiceClient>();
    await Task.WhenAll(
        azureStorageTableNames.Select(
            tableName => tableServiceClient?.CreateTableIfNotExistsAsync(tableName) ?? Task.CompletedTask));
}
#endif

app.Run();
