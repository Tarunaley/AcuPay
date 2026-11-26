using AcuPay.Api.Dtos;
using AcuPay.Domain;
using AcuPay.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcuPay.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = AppRoles.Finance)]
public sealed class ReportsController(IInvoiceService service) : ControllerBase
{
    [HttpGet("spend-by-vendor")]
    public async Task<ActionResult<IReadOnlyList<SpendByVendorDto>>> SpendByVendor(CancellationToken ct)
    {
        var rows = await service.GetSpendByVendorAsync(ct);
        return Ok(rows.Select(x => Mapper.ToDto(x)).ToList());
    }
}
