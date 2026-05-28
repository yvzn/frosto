
using Azure.Data.Tables;
using Azure.Maps.TimeZones;
using Azure.Core.GeoJson;
using Azure;
using tools;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder()
.SetBasePath(Directory.GetCurrentDirectory())
.AddJsonFile("appsettings.Development.json", optional: false);
var config = builder.Build();
var connectionString = config.GetConnectionString("TableStorage")!;
var azureMapsSubscriptionKey = config.GetConnectionString("AzureMaps") ?? string.Empty;

var command = args.Length > 0 ? args[0].ToLowerInvariant() : "backfill";

if (command == "review")
{
	await ReviewBatchAsync(connectionString);
	return;
}

await BackfillAsync(connectionString, azureMapsSubscriptionKey);

// ---- REVIEW ----

async Task ReviewBatchAsync(string connStr)
{
	const int SlotsPerDay = 72;
	const int SlotMinutes = 20;
	const int SampleSize = 5;

	var locationBatchClient = new TableClient(connStr, "locationbatch");
	var validLocationClient = new TableClient(connStr, "validlocation");

	Console.WriteLine("=== locationbatch Review Report ===");
	Console.WriteLine();

	// Load all validlocation entries into a lookup by "PartitionKey|RowKey"
	var locationLookup = new Dictionary<string, LocationEntity>();
	await foreach (var loc in validLocationClient.QueryAsync<LocationEntity>(
	select: ["PartitionKey", "RowKey", "city", "country", "utc_offset_minutes", "hemisphere"]))
	{
		locationLookup[$"{loc.PartitionKey}|{loc.RowKey}"] = loc;
	}

	// Load all locationbatch entries
	var batchEntries = new List<LocationBatchEntity>();
	await foreach (var entry in locationBatchClient.QueryAsync<LocationBatchEntity>())
	{
		batchEntries.Add(entry);
	}

	int total = batchEntries.Count;
	Console.WriteLine($"Valid locations loaded : {locationLookup.Count}");
	Console.WriteLine($"Total batch assignments: {total}");

	if (total == 0)
	{
		Console.WriteLine("No assignments found in locationbatch table.");
		return;
	}

	Console.WriteLine();

	// --- Slot distribution ---
	var slotCounts = new int[SlotsPerDay];
	foreach (var e in batchEntries)
	{
		if (e.slot_index >= 0 && e.slot_index < SlotsPerDay)
			slotCounts[e.slot_index]++;
	}

	int nonEmptySlots = slotCounts.Count(c => c > 0);
	double avgPerSlot = (double)total / SlotsPerDay;
	double slotVariance = slotCounts.Average(c => (c - avgPerSlot) * (c - avgPerSlot));
	double slotStdDev = Math.Sqrt(slotVariance);

	Console.WriteLine("--- Slot Distribution (72 slots x 20 min, starting 00:00 UTC) ---");
	Console.WriteLine($"  Slots with assignments : {nonEmptySlots} / {SlotsPerDay}");
	Console.WriteLine($"  Min per slot : {slotCounts.Min()}");
	Console.WriteLine($"  Max per slot : {slotCounts.Max()}");
	Console.WriteLine($"  Avg per slot : {avgPerSlot:F2}");
	Console.WriteLine($"  Std dev      : {slotStdDev:F2}");
	Console.WriteLine();

	// Hourly histogram: group the 3 slots per hour (slots 3n, 3n+1, 3n+2)
	Console.WriteLine("  Hourly counts (UTC) -- each row = 1 hour (3 slots of 20 min):");
	Console.WriteLine("  Hour  :00  :20  :40  | Bar");
	for (int hour = 0; hour < 24; hour++)
	{
		int s0 = slotCounts[hour * 3];
		int s1 = slotCounts[hour * 3 + 1];
		int s2 = slotCounts[hour * 3 + 2];
		string bar = new string('#', s0 + s1 + s2);
		Console.WriteLine($"  {hour:D2}:xx   {s0,3}  {s1,3}  {s2,3}  | {bar}");
	}
	Console.WriteLine();

	// --- Day distribution ---
	var dayGroups = batchEntries
	.GroupBy(e => e.day_index)
	.OrderBy(g => g.Key)
	.ToList();

	Console.WriteLine("--- Day Distribution ---");
	Console.WriteLine($"  Days with assignments: {dayGroups.Count}");
	foreach (var g in dayGroups)
	{
		Console.WriteLine($"  Day {g.Key,3} : {g.Count(),5} locations");
	}

	if (dayGroups.Count > 0)
	{
		double avgPerDay = (double)total / dayGroups.Count;
		double dayVariance = dayGroups.Average(g => ((double)g.Count() - avgPerDay) * (g.Count() - avgPerDay));
		double dayStdDev = Math.Sqrt(dayVariance);
		Console.WriteLine($"  Avg per day : {avgPerDay:F2}");
		Console.WriteLine($"  Std dev     : {dayStdDev:F2}");
	}
	Console.WriteLine();

	// --- UTC offset alignment ---
	int inWindow = 0;
	int missingOffset = 0;
	var outsideWindow = new List<(string City, string Country, int SlotIndex, int LocalMinutes)>();

	foreach (var entry in batchEntries)
	{
		var key = $"{entry.PartitionKey}|{entry.RowKey}";
		locationLookup.TryGetValue(key, out var loc);

		if (loc?.utc_offset_minutes is null)
			missingOffset++;

		int utcOffset = loc?.utc_offset_minutes ?? 0;
		int slotUtcMinutes = entry.slot_index * SlotMinutes;
		int localMinutes = ((slotUtcMinutes + utcOffset) % 1440 + 1440) % 1440;

		// Target window: 05:00 (300 min) to 08:00 (480 min)
		if (localMinutes >= 300 && localMinutes < 480)
		{
			inWindow++;
		}
		else
		{
			outsideWindow.Add((loc?.city ?? "(unknown)", loc?.country ?? entry.PartitionKey ?? "", entry.slot_index, localMinutes));
		}
	}

	Console.WriteLine("--- Timezone Alignment (target local time: 05:00-08:00) ---");
	Console.WriteLine($"  In window : {inWindow} / {total} ({100.0 * inWindow / total:F1}%)");
	Console.WriteLine($"  Outside   : {outsideWindow.Count} / {total} ({100.0 * outsideWindow.Count / total:F1}%)");
	if (missingOffset > 0)
		Console.WriteLine($"  Missing utc_offset_minutes (treated as UTC+0): {missingOffset}");

	if (outsideWindow.Count > 0)
	{
		int preview = Math.Min(10, outsideWindow.Count);
		Console.WriteLine($"  First {preview} locations outside the window:");
		foreach (var (city, country, slotIdx, localMins) in outsideWindow.Take(preview))
		{
			int utcMins = slotIdx * SlotMinutes;
			Console.WriteLine($"    {city}, {country}  slot {slotIdx,2} -> {utcMins / 60:D2}:{utcMins % 60:D2} UTC -> {localMins / 60:D2}:{localMins % 60:D2} local");
		}
	}
	Console.WriteLine();

	// --- Random sample ---
	Console.WriteLine($"--- Random Sample ({SampleSize} locations) ---");

	var rng = new Random();
	var sample = batchEntries.OrderBy(_ => rng.Next()).Take(SampleSize).ToList();

	foreach (var entry in sample)
	{
		var key = $"{entry.PartitionKey}|{entry.RowKey}";
		locationLookup.TryGetValue(key, out var loc);

		string city = loc?.city ?? "(unknown)";
		string country = loc?.country ?? entry.PartitionKey ?? "";
		string hemisphere = loc?.hemisphere ?? "?";
		int utcOffset = loc?.utc_offset_minutes ?? 0;

		int slotStartUtc = entry.slot_index * SlotMinutes;
		int slotEndUtc = slotStartUtc + SlotMinutes;
		int localStart = ((slotStartUtc + utcOffset) % 1440 + 1440) % 1440;
		int localEnd = ((slotEndUtc + utcOffset) % 1440 + 1440) % 1440;

		string offsetSign = utcOffset >= 0 ? "+" : "-";
		int offsetAbs = Math.Abs(utcOffset);
		string offsetStr = $"UTC{offsetSign}{offsetAbs / 60}:{offsetAbs % 60:D2}";

		Console.WriteLine($"  {city}, {country} [{hemisphere}]");
		Console.WriteLine($"    Day {entry.day_index}  Slot {entry.slot_index}");
		Console.WriteLine($"    UTC  : {slotStartUtc / 60:D2}:{slotStartUtc % 60:D2}-{slotEndUtc / 60:D2}:{slotEndUtc % 60:D2}");
		Console.WriteLine($"    Local: {localStart / 60:D2}:{localStart % 60:D2}-{localEnd / 60:D2}:{localEnd % 60:D2}  ({offsetStr})");
		Console.WriteLine();
	}
}

// ---- BACKFILL ----

async Task BackfillAsync(string connStr, string azureMapsKey)
{
	var tableClient = new TableClient(connStr, "validlocation");

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

		// If both timezone and offset are null, try to get them from Azure Maps
		if (string.IsNullOrWhiteSpace(entity.timezone) && string.IsNullOrWhiteSpace(entity.offset))
		{
			var azureMapsResult = await GetTimezoneFromCoordinatesAsync(entity.coordinates, azureMapsKey);
			if (azureMapsResult.HasValue)
			{
				entity.timezone = azureMapsResult.Value.timezoneId;
				entity.offset = azureMapsResult.Value.offsetString;
			}
		}

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
			Console.WriteLine($"Updated {entity.PartitionKey}|{entity.RowKey}: hemisphere={entity.hemisphere}, utc_offset_minutes={entity.utc_offset_minutes}, count={updated + 1}");
			updated++;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Error updating {entity.PartitionKey}|{entity.RowKey}: {ex.Message}");
			errors++;
		}
	}

	Console.WriteLine($"Done. Updated: {updated}, Skipped (already set): {skipped}, Errors: {errors}");
}

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

	// Fall back to offset string (e.g. "+02:00", "-05:30", "-08:00")
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

static async Task<(string? timezoneId, string? offsetString, int? offsetMinutes)?> GetTimezoneFromCoordinatesAsync(string? coordinates, string azureMapsSubscriptionKey)
{
	if (string.IsNullOrWhiteSpace(coordinates) || string.IsNullOrWhiteSpace(azureMapsSubscriptionKey))
	{
		return null;
	}

	try
	{
		var parts = coordinates.Trim().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length < 2 || !double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double latitude) ||
		    !double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double longitude))
		{
			return null;
		}

		var credential = new AzureKeyCredential(azureMapsSubscriptionKey);
		var client = new MapsTimeZoneClient(credential);

		var options = new GetTimeZoneOptions();
		var geoPosition = new GeoPosition(longitude, latitude);

		var timezoneResult = await client.GetTimeZoneByCoordinatesAsync(geoPosition, options);

		foreach (var timezone in timezoneResult.Value.TimeZones)
		{
			try
			{
				var systemTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timezone.Id);
				var offset = systemTimeZone.GetUtcOffset(DateTimeOffset.UtcNow);
				int offsetMinutes = (int)offset.TotalMinutes;

				// Format offset string as "+HH:MM" or "-HH:MM"
				string offsetSign = offsetMinutes >= 0 ? "+" : "-";
				int offsetAbs = Math.Abs(offsetMinutes);
				string offsetString = $"{offsetSign}{offsetAbs / 60:D2}:{offsetAbs % 60:D2}";

				return (timezone.Id, offsetString, offsetMinutes);
			}
			catch (Exception)
			{
				continue;
			}
		}

		return null;
	}
	catch (Exception ex)
	{
		Console.Error.WriteLine($"Error getting timezone from Azure Maps: {ex.Message}");
		return null;
	}
}
