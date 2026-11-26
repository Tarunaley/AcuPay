using System.ComponentModel.DataAnnotations;
using AcuPay.Domain.Entities;
using AcuPay.Domain.Rules;

namespace AcuPay.Api.Dtos;

public sealed class LoginRequestDto
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;
}

public sealed record LoginResponseDto(string Token, DateTime ExpiresAtUtc, string Username, string Role);

public sealed class CreateInvoiceRequestDto
{
    [Required(ErrorMessage = "Invoice number is required.")]
    [StringLength(50, ErrorMessage = "Invoice number can be at most 50 characters.")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vendor code is required.")]
    [StringLength(20, ErrorMessage = "Vendor code can be at most 20 characters.")]
    public string VendorCode { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "PO number can be at most 30 characters.")]
    public string? PoNumber { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999.99", ErrorMessage = "Invoice amount must be greater than zero.")]
    public decimal InvoiceAmount { get; set; }

    [Required(ErrorMessage = "Invoice date is required.")]
    public DateTime? InvoiceDate { get; set; }

    public NewInvoice ToDomain() => new(
        InvoiceNumber.Trim(),
        VendorCode.Trim(),
        string.IsNullOrWhiteSpace(PoNumber) ? null : PoNumber.Trim(),
        InvoiceAmount,
        InvoiceDate!.Value);
}

public sealed class DecisionRequestDto
{
    [StringLength(500, ErrorMessage = "Reason can be at most 500 characters.")]
    public string? Reason { get; set; }
}

public sealed class InvoiceQueryDto
{
    [StringLength(20)]
    public string? Status { get; set; }

    [StringLength(20)]
    public string? VendorCode { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Page must be 1 or greater.")]
    public int Page { get; set; } = 1;
}

public sealed record InvoiceDto(
    int InvoiceId,
    string InvoiceNumber,
    string VendorCode,
    string VendorName,
    string? PoNumber,
    decimal? PoAmount,
    decimal InvoiceAmount,
    decimal? VariancePercent,
    string Status,
    string? MatchNote,
    string CreatedBy,
    string? DecidedBy,
    string? DecisionReason,
    DateOnly InvoiceDate,
    DateTime CreatedAtUtc,
    DateTime? DecidedAtUtc);

public sealed record PagedResponse<T>(int Page, int PageSize, int TotalCount, IReadOnlyList<T> Items);

public sealed record SpendByVendorDto(string VendorCode, string VendorName, decimal TotalSpend, int InvoiceCount);

public static class Mapper
{
    public static InvoiceDto ToDto(Invoice i) => new(
        i.InvoiceId, i.InvoiceNumber, i.VendorCode, i.VendorName, i.PoNumber, i.PoAmount, i.InvoiceAmount,
        MatchEngine.VarianceFor(i), i.Status, i.MatchNote, i.CreatedBy, i.DecidedBy, i.DecisionReason,
        DateOnly.FromDateTime(i.InvoiceDate), i.CreatedAtUtc, i.DecidedAtUtc);

    public static SpendByVendorDto ToDto(SpendByVendor s) => new(s.VendorCode, s.VendorName, s.TotalSpend, s.InvoiceCount);
}
