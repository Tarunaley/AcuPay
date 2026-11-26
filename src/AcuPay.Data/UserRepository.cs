using AcuPay.Domain.Abstractions;
using AcuPay.Domain.Entities;

namespace AcuPay.Data;

public sealed class UserRepository(SqlConnectionFactory factory) : IUserRepository
{
    public async Task<UserAccount?> GetByUsernameAsync(string username, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.OpenAsync(ct);
        await using var cmd = Db.Proc(conn, "dbo.usp_GetUserByUsername");
        cmd.Parameters.Add(Db.Str("@Username", username, 50));

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return new UserAccount(
            reader.GetInt32(reader.GetOrdinal("UserId")),
            reader.GetString(reader.GetOrdinal("Username")),
            reader.GetString(reader.GetOrdinal("PasswordHash")),
            reader.GetString(reader.GetOrdinal("Role")));
    }
}
