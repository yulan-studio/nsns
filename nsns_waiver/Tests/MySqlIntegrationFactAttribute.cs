using Xunit;

namespace nsns_waiver.Tests;

public sealed class MySqlIntegrationFactAttribute : FactAttribute
{
    public MySqlIntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("WAIVERAPP_TEST_MYSQL_CONNECTION")))
        {
            Skip = "WAIVERAPP_TEST_MYSQL_CONNECTION is not configured; MySQL integration test skipped.";
        }
    }
}
