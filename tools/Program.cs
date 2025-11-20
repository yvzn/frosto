

using System.Text.Json;
using Azure.Data.Tables;
using tools;

var timezoneJson = File.ReadAllText("timezones.json");
var timezones = JsonSerializer.Deserialize<List<Timezone>>(timezoneJson);
ArgumentNullException.ThrowIfNull(timezones);

var timezoneDict =
	(
		from tz in timezones
		where tz.Offset != null && tz.Offset.Sdt != null && tz.Id != null
		select new { tz.Id, tz.Offset!.Sdt }
	).ToDictionary(tz => tz.Id, tz => tz.Sdt);

var tableClient = new TableClient(
	connectionString: "<< YOUR_CONNECTION_STRING_HERE >>",
	tableName: "validlocation");

var entities = tableClient.QueryAsync<LocationEntity>(f => f.country != "France" && f.timezone != "");
await foreach (var entity in entities)
{
	Console.WriteLine($"{entity.city}, {entity.country}, {entity.timezone}, {entity.offset}");
	if (!string.IsNullOrEmpty(entity.offset))
	{
		continue;
	}

	if (entity.timezone != null && timezoneDict.TryGetValue(entity.timezone, out var offset))
	{
		Console.WriteLine("Updating offset to " + offset);
		entity.offset = offset;
	}

	await tableClient.UpdateEntityAsync(entity, entity.ETag);
}

