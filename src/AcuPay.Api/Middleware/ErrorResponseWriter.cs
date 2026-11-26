namespace AcuPay.Api.Middleware;

public sealed record ErrorBody(string Code, string Message, string CorrelationId);
public sealed record ErrorEnvelope(ErrorBody Error);

public static class HttpContextExtensions
{
    public const string CorrelationKey = "CorrelationId";

    public static string GetCorrelationId(this HttpContext context) =>
        context.Items[CorrelationKey] as string ?? context.TraceIdentifier;
}

public static class ErrorResponseWriter
{
    /// <summary>Writes the standard error shape: { "error": { code, message, correlationId } }.</summary>
    public static async Task WriteAsync(HttpContext context, int statusCode, string code, string message)
    {
        if (context.Response.HasStarted) return;

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(
            new ErrorEnvelope(new ErrorBody(code, message, context.GetCorrelationId())));
    }
}
