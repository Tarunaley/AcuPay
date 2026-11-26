using AcuPay.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcuPay.Api.Controllers;

[ApiController]
[Route("api/health")]
[AllowAnonymous]
public sealed class HealthController(IDatabaseHealth database, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var reachable = await database.CanConnectAsync(ct);

        var body = new
        {
            status = reachable ? "Healthy" : "Unhealthy",
            environment = environment.EnvironmentName,
            database = reachable ? "Reachable" : "Unreachable",
            message = reachable ? "All checks passed." : "The database is unreachable."
        };

        return reachable ? Ok(body) : StatusCode(StatusCodes.Status503ServiceUnavailable, body);
    }
}
