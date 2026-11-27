using System.Text.RegularExpressions;
using Azure;
using Azure.Data.Tables;
using Frosto.Entities;
using Microsoft.Extensions.Configuration;

// One-time migration: replace the unsalted SHA-256 email hashes left on
// unsubscribed users by the previous DisableUserAsync with "disabled".

var dryRun = args.Any(argument =>
	string.Equals(argument, "--dry-run", StringComparison.OrdinalIgnoreCase));

var configuration = new ConfigurationBuilder()
	.SetBasePath(Directory.GetCurrentDirectory())
	.AddJsonFile("appsettings.Development.json", optional: true)
	.Build();

var tableEndpoint = Environment.GetEnvironmentVariable("ALERTS_TABLE_ENDPOINT");
var connectionString =
	Environment.GetEnvironmentVariable("ALERTS_CONNECTION_STRING")
	?? Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
	?? configuration.GetConnectionString("TableStorage")
	?? configuration["ConnectionStrings:TableStorage"];

if (string.IsNullOrWhiteSpace(tableEndpoint) && string.IsNullOrWhiteSpace(connectionString))
{
	Console.Error.WriteLine(
		"Table storage configuration not found. Set ALERTS_TABLE_ENDPOINT for managed identity, " +
		"or ALERTS_CONNECTION_STRING or ConnectionStrings:TableStorage for key authentication.");
	return 1;
}

const string tableName = "user";
var tableService = TableServiceClientFactory.Create(tableEndpoint, connectionString);
var userTable = tableService.GetTableClient(tableName);

const string disabledEmail = "disabled";
var sha256Hex = new Regex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant);

var scanned = 0;
var replaced = 0;
var hashedButActive = 0;

await foreach (var entity in userTable.QueryAsync<TableEntity>())
{
	scanned++;

	if (entity.GetString("email") is not { } email || !sha256Hex.IsMatch(email))
	{
		continue;
	}

	// Never print the hash itself: it can be matched against a known address.
	if (entity.GetBoolean("disabled") is not true)
	{
		hashedButActive++;
		Console.Error.WriteLine(
			$"Skipped {entity.PartitionKey}|{entity.RowKey}: hashed email but user is not disabled");
		continue;
	}

	Console.WriteLine(
		$"{(dryRun ? "Would replace" : "Replacing")} hashed email on {entity.PartitionKey}|{entity.RowKey}");

	if (dryRun)
	{
		replaced++;
		continue;
	}

	var patch = new TableEntity(entity.PartitionKey, entity.RowKey)
	{
		["email"] = disabledEmail,
	};

	await userTable.UpdateEntityAsync(
		patch,
		entity.ETag,
		TableUpdateMode.Merge,
		cancellationToken: CancellationToken.None);

	replaced++;
}

Console.WriteLine();
Console.WriteLine(dryRun ? "Dry run complete." : "Migration complete.");
Console.WriteLine($"Scanned: {scanned}");
Console.WriteLine($"{(dryRun ? "Would replace" : "Replaced")}: {replaced}");
Console.WriteLine($"Hashed but not disabled (skipped): {hashedButActive}");

return 0;
