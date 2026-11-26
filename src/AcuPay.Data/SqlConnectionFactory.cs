using Microsoft.Data.SqlClient;

namespace AcuPay.Data;

public sealed class SqlConnectionFactory(string connectionString)
{
    public SqlConnection Create() => new(connectionString);
}
