using AcuPay.Domain;
using AcuPay.Domain.Entities;
using AcuPay.Domain.Settings;

namespace AcuPay.Tests;

internal static class TestData
{
    public static AcuPaySettings Settings() => new()
    {
        PriceTolerancePercent = 2.00m,
        AutoApproveLimit = 50000.00m,
        TokenExpiryMinutes = 60,
        PageSize = 10,
        MinReasonLength = 10
    };

    /// <summary>Invoice for vendor 1 against an open PO of the same vendor, unless overridden.</summary>
    public static Invoice Invoice(
        decimal invoiceAmount = 100m, decimal? poAmount = 100m, int? poId = 1, int vendorId = 1, int? poVendorId = 1,
        string poStatus = PoStatus.Open, string status = InvoiceStatus.Pending, string createdBy = "requester1") => new()
    {
        InvoiceId = 1,
        InvoiceNumber = "INV-T",
        VendorId = vendorId,
        VendorCode = "V001",
        VendorName = "Test Vendor",
        PoId = poId,
        PoNumber = poId is null ? null : "PO-T",
        PoVendorId = poId is null ? null : poVendorId,
        PoAmount = poId is null ? null : poAmount,
        PoStatus = poId is null ? null : poStatus,
        InvoiceAmount = invoiceAmount,
        InvoiceDate = new DateTime(2026, 10, 10),
        Status = status,
        CreatedBy = createdBy
    };
}
