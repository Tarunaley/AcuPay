using AcuPay.Domain.Entities;
using AcuPay.Domain.Settings;

namespace AcuPay.Domain.Rules;

public sealed record MatchOutcome(string Status, string Note, decimal? VariancePercent, bool AutoApproved);

/// <summary>Section 5.1 matching + 5.2 auto-approval. Pure C#: no database, no HTTP.</summary>
public sealed class MatchEngine(AcuPaySettings settings)
{
    public MatchOutcome Evaluate(Invoice invoice)
    {
        // First condition that applies wins.
        if (invoice.PoId is null)
            return Exception(MatchNotes.NoPo);

        if (invoice.PoVendorId != invoice.VendorId)
            return Exception(MatchNotes.VendorMismatch);

        if (string.Equals(invoice.PoStatus, PoStatus.Closed, StringComparison.OrdinalIgnoreCase))
            return Exception(MatchNotes.PoClosed);

        var variance = CalculateVariance(invoice.InvoiceAmount, invoice.PoAmount!.Value);

        if (variance > settings.PriceTolerancePercent)
            return new MatchOutcome(InvoiceStatus.Exception, MatchNotes.OverPoAmount, variance, false);

        // Matched (within tolerance, or under-billed which the business accepts)
        var note = variance < -settings.PriceTolerancePercent ? MatchNotes.UnderPoAmount : MatchNotes.WithinTolerance;

        if (invoice.InvoiceAmount <= settings.AutoApproveLimit)
            return new MatchOutcome(InvoiceStatus.Approved, MatchNotes.AutoApproved, variance, true);

        return new MatchOutcome(InvoiceStatus.Matched, note, variance, false);
    }

    /// <summary>(Invoice - PO) / PO * 100, rounded to 2 decimals before comparison.</summary>
    public static decimal CalculateVariance(decimal invoiceAmount, decimal poAmount) =>
        Math.Round((invoiceAmount - poAmount) / poAmount * 100m, 2, MidpointRounding.AwayFromZero);

    /// <summary>Variance for display; null when "not calculated" (no PO, vendor mismatch, closed PO).</summary>
    public static decimal? VarianceFor(Invoice invoice)
    {
        if (invoice.PoId is null || invoice.PoAmount is null) return null;
        if (invoice.PoVendorId != invoice.VendorId) return null;
        if (string.Equals(invoice.PoStatus, PoStatus.Closed, StringComparison.OrdinalIgnoreCase)) return null;
        return CalculateVariance(invoice.InvoiceAmount, invoice.PoAmount.Value);
    }

    private static MatchOutcome Exception(string note) => new(InvoiceStatus.Exception, note, null, false);
}
