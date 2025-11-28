using ClickHouse.Client.ADO;
using Microsoft.Extensions.Options;

namespace Tracking.Api.Data;

public sealed class ClickHouseConnectionFactory
{
    private readonly string _connectionString;

    public ClickHouseConnectionFactory(IOptions<ClickHouseOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public async Task<ClickHouseConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
