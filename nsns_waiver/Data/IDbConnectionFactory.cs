using MySqlConnector;

namespace nsns_waiver.Data;

public interface IDbConnectionFactory
{
    Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
