
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

int updated = 0;
int skipped = 0;
int errors = 0;

await foreach (var entity in tableClient.QueryAsync<LocationEntity>())
{
	if (entity is null)
	{
		continue;
	}

	var newHemisphere = DeriveHemisphere(entity.coordinates);
	var newUtcOffsetMinutes = DeriveUtcOffsetMinutes(entity.timezone, entity.offset);

	if (entity.hemisphere == newHemisphere && entity.utc_offset_minutes == newUtcOffsetMinutes)
	{
		skipped++;
		continue;
	}

	entity.hemisphere = newHemisphere;
	entity.utc_offset_minutes = newUtcOffsetMinutes;

	try
	{
		await tableClient.UpdateEntityAsync(entity, entity.ETag);
		Console.WriteLine($"Updated {entity.PartitionKey}|{entity.RowKey}: hemisphere={entity.hemisphere}, utc_offset_minutes={entity.utc_offset_minutes}");
		updated++;
	}
	catch (Exception ex)
	{
		Console.Error.WriteLine($"Error updating {entity.PartitionKey}|{entity.RowKey}: {ex.Message}");
		errors++;
	}
}

Console.WriteLine($"Done. Updated: {updated}, Skipped (already set): {skipped}, Errors: {errors}");

static string? DeriveHemisphere(string? coordinates)
{
	if (string.IsNullOrWhiteSpace(coordinates))
	{
		return null;
	}

	var parts = coordinates.Trim().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
	if (parts.Length >= 1 && double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double latitude))
	{
		return latitude >= 0 ? "N" : "S";
	}

	return null;
}

static int? DeriveUtcOffsetMinutes(string? timezone, string? offset)
{
	// Try IANA timezone id first
	if (!string.IsNullOrWhiteSpace(timezone))
	{
		try
		{
			var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone.Trim());
			var span = tz.GetUtcOffset(DateTimeOffset.UtcNow);
			return (int)span.TotalMinutes;
		}
		catch (TimeZoneNotFoundException) { }
		catch (InvalidTimeZoneException) { }
	}

	// Fall back to offset string (e.g. "+02:00", "-05:30", "−08:00")
	if (!string.IsNullOrWhiteSpace(offset))
	{
		var trimmed = offset.Trim();
		var sign = trimmed.StartsWith('-') || trimmed.StartsWith('\u2212') ? -1 : 1;
		var parts = trimmed.TrimStart('+', '-', '\u2212').Split(':');
		if (parts.Length >= 1 && int.TryParse(parts[0], out int hours))
		{
			int mins = 0;
			if (parts.Length >= 2) int.TryParse(parts[1], out mins);
			return sign * (hours * 60 + mins);
		}
	}

	return null;
}

