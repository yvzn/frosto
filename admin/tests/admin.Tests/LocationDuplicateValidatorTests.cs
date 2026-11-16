using admin.Models;
using admin.Services;

namespace admin.Tests;

public class LocationDuplicateValidatorTests
{
    [Fact]
    public void RequiresDuplicateConfirmation_WhenDuplicateExistsAndUnconfirmed_ReturnsTrue()
    {
        var location = new Location { city = "Paris", country = "France" };

        var result = LocationDuplicateValidator.RequiresDuplicateConfirmation(location, "FR-PARIS", false);

        Assert.True(result);
    }

    [Fact]
    public void RequiresDuplicateConfirmation_WhenConfirmed_ReturnsFalse()
    {
        var location = new Location { city = "Paris", country = "France" };

        var result = LocationDuplicateValidator.RequiresDuplicateConfirmation(location, "FR-PARIS", true);

        Assert.False(result);
    }

    [Fact]
    public void RequiresDuplicateConfirmation_WhenMissingLocationData_ReturnsFalse()
    {
        var location = new Location { city = "", country = "France" };

        var result = LocationDuplicateValidator.RequiresDuplicateConfirmation(location, "FR-PARIS", false);

        Assert.False(result);
    }

    [Fact]
    public void GetDuplicateMessage_UsesNormalizedCityAndCountry()
    {
        var location = new Location { city = " PARIS ", country = " france " };

        var message = LocationDuplicateValidator.GetDuplicateMessage(location, "FR-PARIS");

        Assert.Contains("Paris", message);
        Assert.Contains("France", message);
        Assert.Contains("FR-PARIS", message);
    }
}
