using AcuPay.Api.Dtos;
using AcuPay.Domain;
using AcuPay.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcuPay.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public sealed class InvoicesController(IInvoiceService service) : ControllerBase
{
    private string Username => User.Identity?.Name ?? string.Empty;
    private string Role => User.FindFirst("role")?.Value ?? string.Empty;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<InvoiceDto>>> List([FromQuery] InvoiceQueryDto query, CancellationToken ct)
    {
        var result = await service.GetPageAsync(query.Status, query.VendorCode, query.Page, ct);
        return Ok(new PagedResponse<InvoiceDto>(
            result.Page, result.PageSize, result.TotalCount, result.Items.Select(x => Mapper.ToDto(x)).ToList()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceDto>> Get(int id, CancellationToken ct) =>
        Ok(Mapper.ToDto(await service.GetAsync(id, ct)));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Requester},{AppRoles.Finance}")]
    public async Task<ActionResult<InvoiceDto>> Create([FromBody] CreateInvoiceRequestDto request, CancellationToken ct)
    {
        var created = Mapper.ToDto(await service.CreateAsync(request.ToDomain(), Username, ct));
        return CreatedAtAction(nameof(Get), new { id = created.InvoiceId }, created);
    }

    [HttpPost("{id:int}/match")]
    [Authorize(Roles = $"{AppRoles.Requester},{AppRoles.Finance}")]
    public async Task<ActionResult<InvoiceDto>> Match(int id, CancellationToken ct) =>
        Ok(Mapper.ToDto(await service.MatchAsync(id, Username, ct)));

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = $"{AppRoles.Approver},{AppRoles.Finance}")]
    public async Task<ActionResult<InvoiceDto>> Approve(
        int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DecisionRequestDto? request, CancellationToken ct) =>
        Ok(Mapper.ToDto(await service.ApproveAsync(id, Username, Role, request?.Reason, ct)));

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = $"{AppRoles.Approver},{AppRoles.Finance}")]
    public async Task<ActionResult<InvoiceDto>> Reject(
        int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DecisionRequestDto? request, CancellationToken ct) =>
        Ok(Mapper.ToDto(await service.RejectAsync(id, Username, Role, request?.Reason, ct)));
}
