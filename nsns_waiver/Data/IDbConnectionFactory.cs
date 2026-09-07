using MySqlConnector;

namespace nsns_waiver.Data;

/// <summary>
/// Creates open MySQL connections using the application's configured settings.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates and opens a new connection that the caller must dispose.
    /// </summary>
    Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
