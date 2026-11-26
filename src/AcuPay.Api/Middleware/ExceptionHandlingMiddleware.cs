using AcuPay.Domain;
using AcuPay.Domain.Exceptions;

namespace AcuPay.Api.Middleware;

/// <summary>The only place exceptions are caught. No stack trace ever reaches the client.</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning("Request was cancelled by the client");
        }
        catch (AppException ex)
        {
            logger.LogWarning("Business rule failed: {Code} - {Message}", ex.Code, ex.Message);
            await ErrorResponseWriter.WriteAsync(context, ex.StatusCode, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await ErrorResponseWriter.WriteAsync(context, StatusCodes.Status500InternalServerError,
                ErrorCodes.InternalError, "An unexpected error occurred. Quote the correlation id when reporting it.");
        }
    }
}
