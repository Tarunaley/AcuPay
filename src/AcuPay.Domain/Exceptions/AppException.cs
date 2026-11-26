namespace AcuPay.Domain.Exceptions;

/// <summary>A business or validation failure. The exception middleware turns it into the standard error shape.</summary>
public sealed class AppException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}
