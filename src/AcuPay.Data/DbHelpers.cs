using System.Data;
using AcuPay.Domain;
using AcuPay.Domain.Exceptions;
using Microsoft.Data.SqlClient;

namespace AcuPay.Data;

/// <summary>Stored procedure calls only; every value goes through a typed SqlParameter.</summary>
internal static class Db
{
    public static SqlCommand Proc(SqlConnection connection, string name) =>
        new(name, connection) { CommandType = CommandType.StoredProcedure };

    public static SqlParameter Str(string name, string? value, int size) =>
        new(name, SqlDbType.NVarChar, size) { Value = (object?)value ?? DBNull.Value };

    public static SqlParameter Int(string name, int value) =>
        new(name, SqlDbType.Int) { Value = value };

    public static SqlParameter Money(string name, decimal value) =>
        new(name, SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = value };

    public static SqlParameter Date(string name, DateTime value) =>
        new(name, SqlDbType.Date) { Value = value.Date };
}

internal static class ReaderExtensions
{
    public static string? GetStringOrNull(this SqlDataReader r, string column)
    {
        var o = r.GetOrdinal(column);
        return r.IsDBNull(o) ? null : r.GetString(o);
    }

    public static int? GetIntOrNull(this SqlDataReader r, string column)
    {
        var o = r.GetOrdinal(column);
        return r.IsDBNull(o) ? null : r.GetInt32(o);
    }

    public static decimal? GetDecimalOrNull(this SqlDataReader r, string column)
    {
        var o = r.GetOrdinal(column);
        return r.IsDBNull(o) ? null : r.GetDecimal(o);
    }

    public static DateTime? GetUtcOrNull(this SqlDataReader r, string column)
    {
        var o = r.GetOrdinal(column);
        return r.IsDBNull(o) ? null : DateTime.SpecifyKind(r.GetDateTime(o), DateTimeKind.Utc);
    }
}

/// <summary>Maps SQL Server errors (constraints and THROW codes in the procedures) to AppException.</summary>
internal static class SqlErrors
{
    public static bool IsKnown(SqlException ex) => ex.Number is 2627 or 2601 or 50001 or 50002 or 50003 or 50010;

    public static AppException ToAppException(SqlException ex) => ex.Number switch
    {
        2627 or 2601 => new AppException(ErrorCodes.DuplicateInvoice, "This vendor has already billed that invoice number.", 409),
        50001 => new AppException(ErrorCodes.VendorNotFound, "The vendor code does not exist.", 400),
        50002 => new AppException(ErrorCodes.VendorInactive, "The vendor is inactive and cannot be invoiced.", 400),
        50003 => new AppException(ErrorCodes.PoNotFound, "The purchase order number does not exist.", 400),
        50010 => new AppException(ErrorCodes.InvalidState, "The invoice is not in a state that allows this action.", 409),
        _ => new AppException(ErrorCodes.InternalError, "A database error occurred.", 500)
    };
}
