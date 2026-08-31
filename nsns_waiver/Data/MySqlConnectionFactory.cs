using MySqlConnector;

namespace nsns_waiver.Data;

/// <summary>
/// Validates the configured MySQL connection string and opens UTC-aware connections.
/// </summary>
public sealed class MySqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// Reads and validates ConnectionStrings:Default once during construction.
    /// </summary>
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

    /// <summary>
    /// Opens a new MySQL connection and disposes it if opening fails.
    /// </summary>
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
