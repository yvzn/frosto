using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;

var dryRun = args.Any(argument =>
	string.Equals(argument, "--dry-run", StringComparison.OrdinalIgnoreCase));

var configuration = new ConfigurationBuilder()
	.SetBasePath(Directory.GetCurrentDirectory())
	.AddJsonFile("appsettings.Development.json", optional: true)
	.Build();

var connectionString =
	Environment.GetEnvironmentVariable("ALERTS_CONNECTION_STRING")
	?? Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
	?? configuration.GetConnectionString("TableStorage")
	?? configuration["ConnectionStrings:TableStorage"];

if (string.IsNullOrWhiteSpace(connectionString))
{
	Console.Error.WriteLine(
		"Table storage connection string not found. Set ALERTS_CONNECTION_STRING or " +
		"ConnectionStrings:TableStorage.");
	return 1;
}

const string tableName = "user";
var tableService = new TableServiceClient(connectionString);
var userTable = tableService.GetTableClient(tableName);

var scanned = 0;
var converted = 0;
var alreadyBoolean = 0;
var invalid = 0;

await foreach (var entity in userTable.QueryAsync<TableEntity>())
{
	scanned++;

	if (!entity.TryGetValue("disabled", out var value))
	{
		continue;
	}

	if (value is bool)
	{
		alreadyBoolean++;
		continue;
	}

	if (value is not string disabledText ||
		!bool.TryParse(disabledText.Trim(), out var disabled))
	{
		invalid++;
		Console.Error.WriteLine(
			$"Skipped {entity.PartitionKey}|{entity.RowKey}: disabled={value ?? "<null>"} ({value?.GetType().Name ?? "null"})");
		continue;
	}

	Console.WriteLine(
		$"{(dryRun ? "Would convert" : "Converting")} " +
		$"{entity.PartitionKey}|{entity.RowKey}: \"{disabledText}\" -> {disabled.ToString().ToLowerInvariant()}");

	if (dryRun)
	{
		converted++;
		continue;
	}

	entity["disabled"] = disabled;

	await userTable.UpdateEntityAsync(
		entity,
		entity.ETag,
		TableUpdateMode.Merge,
		cancellationToken: CancellationToken.None);

	converted++;
}

Console.WriteLine();
Console.WriteLine(dryRun ? "Dry run complete." : "Migration complete.");
Console.WriteLine($"Scanned: {scanned}");
Console.WriteLine($"Converted: {converted}");
Console.WriteLine($"Already boolean: {alreadyBoolean}");
Console.WriteLine($"Invalid or unsupported: {invalid}");

return 0;
