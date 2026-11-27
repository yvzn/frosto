using admin.Pages;

namespace admin.Tests.Pages;

public class SignUpModelTests
{
    [Theory]
    [InlineData("United states", "United states of america")]
    [InlineData("united states", "United states of america")]
    [InlineData("United States", "United states of america")]
    [InlineData("UNITED STATES", "United states of america")]
    [InlineData("Belgium", "Belgique")]
    [InlineData("Germany", "Deutschland")]
    public void GeocodingCountryTranslations_MapsCountryCaseInsensitively(string country, string expected)
    {
        var found = SignUpModel.GeocodingCountryTranslations.TryGetValue(country, out var translation);

        Assert.True(found);
        Assert.Equal(expected, translation);
    }

    [Fact]
    public void GeocodingCountryTranslations_DoesNotContainUnknownCountry()
    {
        var found = SignUpModel.GeocodingCountryTranslations.TryGetValue("France", out _);

        Assert.False(found);
    }
}
