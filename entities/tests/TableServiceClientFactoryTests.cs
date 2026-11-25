using Frosto.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace entities_tests;

public class TableServiceClientFactoryTests
{
    [Fact]
    public void AddAlertsTableServiceClient_UsesEndpoint_WhenConfigured()
    {
        var services = new ServiceCollection();

        services.AddAlertsTableServiceClient(
            "https://managedidentity.table.core.windows.net",
            "AccountName=fallback;AccountKey=ZmFrZQ==;EndpointSuffix=core.windows.net");

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<Azure.Data.Tables.TableServiceClient>();

        Assert.Equal(new Uri("https://managedidentity.table.core.windows.net"), client.Uri);
    }

    [Fact]
    public void AddAlertsTableServiceClient_UsesConnectionString_WhenEndpointIsNotConfigured()
    {
        var services = new ServiceCollection();

        services.AddAlertsTableServiceClient(
            null,
            "AccountName=fallback;AccountKey=ZmFrZQ==;EndpointSuffix=core.windows.net");

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<Azure.Data.Tables.TableServiceClient>();

        Assert.Equal("https://fallback.table.core.windows.net/", client.Uri.AbsoluteUri);
    }
}
