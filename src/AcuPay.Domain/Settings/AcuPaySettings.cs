namespace AcuPay.Domain.Settings;

/// <summary>Business constants. Values live in appsettings.json ("AcuPay" section) - never as literals in logic.</summary>
public sealed class AcuPaySettings
{
    public decimal PriceTolerancePercent { get; set; }
    public decimal AutoApproveLimit { get; set; }
    public int TokenExpiryMinutes { get; set; }
    public int PageSize { get; set; }
    public int MinReasonLength { get; set; }

    public void Validate()
    {
        if (PriceTolerancePercent <= 0 || AutoApproveLimit <= 0 || TokenExpiryMinutes <= 0 || PageSize <= 0 || MinReasonLength <= 0)
            throw new InvalidOperationException("All 'AcuPay' settings must be present and greater than zero.");
    }
}
