using admin.Models;
using Azure.Data.Tables;

namespace admin.Services;

public interface ILocationService
{
	public Task<IList<Location>> GetValidLocations();
}

public class LocationService : ILocationService
{
	private TableClient _validLocationTableClient;

	public LocationService(IConfiguration configuration)
	{
		_validLocationTableClient = new TableClient(
			connectionString: configuration.GetConnectionString("Alerts"),
			tableName: "validlocation");
	}

	public async Task<IList<Location>> GetValidLocations()
	{
		var locationEntities = _validLocationTableClient.QueryAsync<LocationEntity>(_ => true);
		var result = new List<Location>();

		await foreach (var locationEntity in locationEntities)
		{
			result.Add(new()
			{
				city = locationEntity.city,
				country = locationEntity.country,
				coordinates = locationEntity.coordinates,
				PartitionKey = locationEntity.PartitionKey,
				RowKey = locationEntity.RowKey,
			});
		}

		return result;
	}
}

