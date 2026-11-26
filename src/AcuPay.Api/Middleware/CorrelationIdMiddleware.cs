namespace AcuPay.Api.Middleware;

/// <summary>Generates a correlation id per request, puts it in the log scope and the response header.</summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString();
        context.Items[HttpContextExtensions.CorrelationKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            logger.LogInformation("{Method} {Path} started", context.Request.Method, context.Request.Path);
            await next(context);
            logger.LogInformation("{Method} {Path} finished with {StatusCode}",
                context.Request.Method, context.Request.Path, context.Response.StatusCode);
        }
    }
}
