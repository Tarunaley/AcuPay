namespace AcuPay.Api.Security;

/// <summary>Issuer and audience come from appsettings.json; Key comes from user-secrets / environment only.</summary>
public sealed class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}
