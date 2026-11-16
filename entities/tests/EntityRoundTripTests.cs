using Xunit;
using Frosto.Entities;

namespace entities_tests;

public class EntityRoundTripTests
{
    [Fact]
    public void LocationEntity_RoundTrips_AllColumns()
    {
        var entity = new LocationEntity
        {
            PartitionKey = "France",
            RowKey = "Paris",
            city = "Paris",
            country = "France",
            coordinates = "48.8566,2.3522",
            users = "test@example.com,another@example.com",
            uat = true,
            disabled = false,
            channel = "web",
            zipCode = "75000",
            lang = "fr",
            timezone = "Europe/Paris",
            offset = "+01:00",
            temperatureUnit = "C",
            minThreshold = 0.5,
            minTemperatureAdjustment = -1.5,
            weatherApiUrl = "https://example.test/{0}/{1}",
            appEnabled = true,
            utc_offset_minutes = 60,
            hemisphere = "N",
        };

        var tableEntity = entity.ToTableEntity();

        Assert.Equal("Paris", entity.RowKey);
        Assert.Equal(true, tableEntity["uat"] as bool?);
        Assert.Equal(false, tableEntity["disabled"] as bool?);
        Assert.Equal(0.5, tableEntity["minThreshold"] as double?);
        Assert.Equal(-1.5, tableEntity["minTemperatureAdjustment"] as double?);
        Assert.Equal("https://example.test/{0}/{1}", tableEntity["weatherApiUrl"] as string);
        Assert.Equal(true, tableEntity["appEnabled"] as bool?);
        Assert.Equal(60, tableEntity["utc_offset_minutes"] as int?);
        Assert.Equal("N", tableEntity["hemisphere"] as string);
    }

    [Fact]
    public void UserEntity_Disabled_IsBoolAndCreated_RemainsString()
    {
        var entity = new UserEntity
        {
            PartitionKey = "users",
            RowKey = "abc",
            email = "alice@example.com",
            disabled = true,
            created = "2025-01-01T00:00:00Z",
            userConsent = "true",
            lang = "fr",
        };

        var tableEntity = entity.ToTableEntity();

        Assert.Equal(typeof(bool), tableEntity["disabled"]?.GetType());
        Assert.Equal(typeof(string), tableEntity["created"]?.GetType());
        Assert.Equal(typeof(string), tableEntity["email"]?.GetType());
    }

    [Fact]
    public void LegacyNullableFields_AreSupported_WhenMissing()
    {
        var location = new LocationEntity
        {
            PartitionKey = "France",
            RowKey = "Paris",
            city = "Paris",
            country = "France",
        };

        var tableEntity = location.ToTableEntity();

        Assert.False(tableEntity.ContainsKey("disabled"));
        Assert.False(tableEntity.ContainsKey("appEnabled"));
        Assert.False(tableEntity.ContainsKey("minThreshold"));
        Assert.False(tableEntity.ContainsKey("weatherApiUrl"));
    }

    [Fact]
    public void UnsubscribeEntity_PreservesTokenField()
    {
        var entity = new UnsubscribeEntity
        {
            PartitionKey = "France",
            RowKey = "token-1",
            token = "abc123",
            user = "user@example.com",
            email = "user@example.com",
            locid = "France|Paris",
        };

        var tableEntity = entity.ToTableEntity();

        Assert.Equal("abc123", tableEntity["token"] as string);
        Assert.Equal("France|Paris", tableEntity["locid"] as string);
    }
}
