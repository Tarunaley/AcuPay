namespace AcuPay.Domain.Entities;

public sealed class Invoice
{
    public int InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public int VendorId { get; init; }
    public string VendorCode { get; init; } = string.Empty;
    public string VendorName { get; init; } = string.Empty;
    public int? PoId { get; init; }
    public string? PoNumber { get; init; }
    public int? PoVendorId { get; init; }
    public decimal? PoAmount { get; init; }
    public string? PoStatus { get; init; }
    public decimal InvoiceAmount { get; init; }
    public DateTime InvoiceDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? MatchNote { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public string? DecidedBy { get; init; }
    public DateTime? DecidedAtUtc { get; init; }
    public string? DecisionReason { get; init; }
}

public sealed record NewInvoice(string InvoiceNumber, string VendorCode, string? PoNumber, decimal InvoiceAmount, DateTime InvoiceDate);

public sealed record UserAccount(int UserId, string Username, string PasswordHash, string Role);

public sealed record SpendByVendor(string VendorCode, string VendorName, decimal TotalSpend, int InvoiceCount);

public sealed record PagedResult<T>(int Page, int PageSize, int TotalCount, IReadOnlyList<T> Items);
