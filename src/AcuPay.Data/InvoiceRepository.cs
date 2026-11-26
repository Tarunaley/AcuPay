using AcuPay.Domain.Abstractions;
using AcuPay.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace AcuPay.Data;

public sealed class InvoiceRepository(SqlConnectionFactory factory) : IInvoiceRepository
{
    public async Task<PagedResult<Invoice>> GetPageAsync(string? status, string? vendorCode, int page, int pageSize, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.OpenAsync(ct);
        await using var cmd = Db.Proc(conn, "dbo.usp_GetInvoices");
        cmd.Parameters.Add(Db.Str("@Status", status, 20));
        cmd.Parameters.Add(Db.Str("@VendorCode", vendorCode, 20));
        cmd.Parameters.Add(Db.Int("@PageNumber", page));
        cmd.Parameters.Add(Db.Int("@PageSize", pageSize));

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var total = 0;
        if (await reader.ReadAsync(ct)) total = reader.GetInt32(0);

        await reader.NextResultAsync(ct);
        var items = new List<Invoice>();
        while (await reader.ReadAsync(ct)) items.Add(Map(reader));

        return new PagedResult<Invoice>(page, pageSize, total, items);
    }

    public async Task<Invoice?> GetByIdAsync(int invoiceId, CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.OpenAsync(ct);
        await using var cmd = Db.Proc(conn, "dbo.usp_GetInvoiceById");
        cmd.Parameters.Add(Db.Int("@InvoiceId", invoiceId));

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<int> InsertAsync(NewInvoice invoice, string createdBy, CancellationToken ct)
    {
        try
        {
            await using var conn = factory.Create();
            await conn.OpenAsync(ct);
            await using var cmd = Db.Proc(conn, "dbo.usp_InsertInvoice");
            cmd.Parameters.Add(Db.Str("@InvoiceNumber", invoice.InvoiceNumber, 50));
            cmd.Parameters.Add(Db.Str("@VendorCode", invoice.VendorCode, 20));
            cmd.Parameters.Add(Db.Str("@PoNumber", invoice.PoNumber, 30));
            cmd.Parameters.Add(Db.Money("@InvoiceAmount", invoice.InvoiceAmount));
            cmd.Parameters.Add(Db.Date("@InvoiceDate", invoice.InvoiceDate));
            cmd.Parameters.Add(Db.Str("@CreatedBy", createdBy, 50));

            var result = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt32(result);
        }
        catch (SqlException ex) when (SqlErrors.IsKnown(ex))
        {
            throw SqlErrors.ToAppException(ex);
        }
    }

    public async Task UpdateMatchAsync(int invoiceId, string status, string matchNote, string? decidedBy, string performedBy, CancellationToken ct)
    {
        try
        {
            await using var conn = factory.Create();
            await conn.OpenAsync(ct);
            await using var cmd = Db.Proc(conn, "dbo.usp_UpdateInvoiceMatch");
            cmd.Parameters.Add(Db.Int("@InvoiceId", invoiceId));
            cmd.Parameters.Add(Db.Str("@Status", status, 20));
            cmd.Parameters.Add(Db.Str("@MatchNote", matchNote, 50));
            cmd.Parameters.Add(Db.Str("@DecidedBy", decidedBy, 50));
            cmd.Parameters.Add(Db.Str("@PerformedBy", performedBy, 50));
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (SqlException ex) when (SqlErrors.IsKnown(ex))
        {
            throw SqlErrors.ToAppException(ex);
        }
    }

    public async Task DecideAsync(int invoiceId, string status, string decidedBy, string? reason, CancellationToken ct)
    {
        try
        {
            await using var conn = factory.Create();
            await conn.OpenAsync(ct);
            await using var cmd = Db.Proc(conn, "dbo.usp_DecideInvoice");
            cmd.Parameters.Add(Db.Int("@InvoiceId", invoiceId));
            cmd.Parameters.Add(Db.Str("@Status", status, 20));
            cmd.Parameters.Add(Db.Str("@DecidedBy", decidedBy, 50));
            cmd.Parameters.Add(Db.Str("@Reason", reason, 500));
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (SqlException ex) when (SqlErrors.IsKnown(ex))
        {
            throw SqlErrors.ToAppException(ex);
        }
    }

    public async Task<IReadOnlyList<SpendByVendor>> GetSpendByVendorAsync(CancellationToken ct)
    {
        await using var conn = factory.Create();
        await conn.OpenAsync(ct);
        await using var cmd = Db.Proc(conn, "dbo.usp_GetSpendByVendor");
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var rows = new List<SpendByVendor>();
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new SpendByVendor(
                reader.GetString(reader.GetOrdinal("VendorCode")),
                reader.GetString(reader.GetOrdinal("VendorName")),
                reader.GetDecimal(reader.GetOrdinal("TotalSpend")),
                reader.GetInt32(reader.GetOrdinal("InvoiceCount"))));
        }
        return rows;
    }

    private static Invoice Map(SqlDataReader r) => new()
    {
        InvoiceId = r.GetInt32(r.GetOrdinal("InvoiceId")),
        InvoiceNumber = r.GetString(r.GetOrdinal("InvoiceNumber")),
        VendorId = r.GetInt32(r.GetOrdinal("VendorId")),
        VendorCode = r.GetString(r.GetOrdinal("VendorCode")),
        VendorName = r.GetString(r.GetOrdinal("VendorName")),
        PoId = r.GetIntOrNull("PoId"),
        PoNumber = r.GetStringOrNull("PoNumber"),
        PoVendorId = r.GetIntOrNull("PoVendorId"),
        PoAmount = r.GetDecimalOrNull("PoAmount"),
        PoStatus = r.GetStringOrNull("PoStatus"),
        InvoiceAmount = r.GetDecimal(r.GetOrdinal("InvoiceAmount")),
        InvoiceDate = r.GetDateTime(r.GetOrdinal("InvoiceDate")),
        Status = r.GetString(r.GetOrdinal("Status")),
        MatchNote = r.GetStringOrNull("MatchNote"),
        CreatedBy = r.GetString(r.GetOrdinal("CreatedBy")),
        CreatedAtUtc = DateTime.SpecifyKind(r.GetDateTime(r.GetOrdinal("CreatedAtUtc")), DateTimeKind.Utc),
        DecidedBy = r.GetStringOrNull("DecidedBy"),
        DecidedAtUtc = r.GetUtcOrNull("DecidedAtUtc"),
        DecisionReason = r.GetStringOrNull("DecisionReason")
    };
}
