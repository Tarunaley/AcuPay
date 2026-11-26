using AcuPay.Domain.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace AcuPay.Data;

public sealed class DatabaseHealth(SqlConnectionFactory factory, ILogger<DatabaseHealth> logger) : IDatabaseHealth
{
    public async Task<bool> CanConnectAsync(CancellationToken ct)
    {
        try
        {
            await using var conn = factory.Create();
            await conn.OpenAsync(ct);
            return true;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            logger.LogError(ex, "Database health check failed");
            return false;
        }
    }
}
