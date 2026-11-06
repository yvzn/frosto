using Azure;
using Azure.Data.Tables;

namespace Frosto.Entities;

public abstract class EntityBase : ITableEntity
{
    public string? PartitionKey { get; set; }
    public string? RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}

public class LocationEntity : weather.ILocation, ITableEntity
{
    public string? city { get; set; }
    public string? country { get; set; }
    public string? coordinates { get; set; }
    public string? timezone { get; set; }
    public string? offset { get; set; }
    public string? users { get; set; }
    public bool? uat { get; set; }
    public bool? disabled { get; set; }
    public string? channel { get; set; }
    public string? lang { get; set; }
    public string? temperatureUnit { get; set; }
    public double? minThreshold { get; set; }
    public double? minTemperatureAdjustment { get; set; }
    public string? weatherApiUrl { get; set; }
    public bool? appEnabled { get; set; }
    public int? utc_offset_minutes { get; set; }
    public string? hemisphere { get; set; }
    public string? zipCode { get; set; }
    public string? PartitionKey { get; set; }
    public string? RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}

public class LocationBatchEntity : ITableEntity
{
    public int slot_index { get; set; }
    public int day_index { get; set; }
    public string? PartitionKey { get; set; }
    public string? RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}

public class MonitoringEntity : ITableEntity
{
    public string? coordinates { get; set; }
    public int? daysUntilNextFrost { get; set; }
    public string? PartitionKey { get; set; }
    public string? RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}

public class BatchEntity : ITableEntity
{
    public string? locations { get; set; }
    public string? PartitionKey { get; set; }
    public string? RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}

public class UserEntity : EntityBase
{
    public string? email { get; set; }
    public bool? disabled { get; set; }
    public string? created { get; set; }
    public string? userConsent { get; set; }
    public string? lang { get; set; }
}

public class SignUpEntity : EntityBase
{
    public string? city { get; set; }
    public string? country { get; set; }
    public string? lang { get; set; }
    public string? temperatureUnit { get; set; }
}

public class CheckSubscriptionEntity : EntityBase
{
    public string? email { get; set; }
    public string? userConsent { get; set; }
    public string? lang { get; set; }
    public string? source { get; set; }
    public string? reason { get; set; }
}

public class UnsubscribeEntity : EntityBase
{
    public string? token { get; set; }
    public string? user { get; set; }
    public string? email { get; set; }
    public string? locid { get; set; }
    public string? reason { get; set; }
    public string? origin { get; set; }
    public string? lang { get; set; }
}

public static class EntityTableExtensions
{
    public static TableEntity ToTableEntity(this ITableEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var properties = entity.GetType()
            .GetProperties()
            .Where(p => p.Name is not ("PartitionKey" or "RowKey" or "Timestamp" or "ETag"));

        var dictionary = new Dictionary<string, object?>();
        foreach (var property in properties)
        {
            var value = property.GetValue(entity);
            if (value is not null)
            {
                dictionary[property.Name] = value;
            }
        }

        return new TableEntity(dictionary);
    }
}
