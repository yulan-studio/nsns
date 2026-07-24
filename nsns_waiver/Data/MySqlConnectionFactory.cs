using MySqlConnector;

namespace nsns_waiver.Data;

public sealed class MySqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public MySqlConnectionFactory(IConfiguration configuration)
    {
        var configuredConnectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "The required connection string 'ConnectionStrings:Default' is missing.");

        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            throw new InvalidOperationException(
                "The required connection string 'ConnectionStrings:Default' is empty.");
        }

        var builder = new MySqlConnectionStringBuilder(configuredConnectionString)
        {
            DateTimeKind = MySqlDateTimeKind.Utc,
            GuidFormat = MySqlGuidFormat.None
        };
        _connectionString = builder.ConnectionString;
    }

    public async Task<MySqlConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = new MySqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
