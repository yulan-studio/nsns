using Microsoft.Extensions.Configuration;
using nsns_waiver.Data;

namespace nsns_waiver.Tests;

public sealed class ConnectionFactoryTests
{
    [Fact]
    public void Constructor_ThrowsWhenConnectionStringIsMissing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => new MySqlConnectionFactory(configuration));

        Assert.Contains("ConnectionStrings:Default", exception.Message);
    }

    [Fact]
    public void Constructor_ThrowsWhenConnectionStringIsEmpty()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ""
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => new MySqlConnectionFactory(configuration));

        Assert.Contains("empty", exception.Message);
    }
}
