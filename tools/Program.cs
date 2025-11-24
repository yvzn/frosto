

using Azure.Data.Tables;
using tools;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder()
	.SetBasePath(Directory.GetCurrentDirectory())
	.AddJsonFile("appsettings.Development.json", optional: false);
var config = builder.Build();
var connectionString = config.GetConnectionString("TableStorage");

var tableClient = new TableClient(
	connectionString: connectionString,
	tableName: "validlocation");

var entities = tableClient.QueryAsync<LocationEntity>(f => f.channel == "tipimail");
await foreach (var entity in entities)
{
	if (entity is null || entity.users == null)
	{
		continue;
	}

	if (!entity.users.Contains("yahoo.fr"))
	{
		continue;
	}

	Console.WriteLine($"{entity.PartitionKey} - {entity.RowKey} - {entity.users}");
}

