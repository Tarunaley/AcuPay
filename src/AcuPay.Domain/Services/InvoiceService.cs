using AcuPay.Domain.Abstractions;
using AcuPay.Domain.Entities;
using AcuPay.Domain.Exceptions;
using AcuPay.Domain.Rules;
using AcuPay.Domain.Settings;

namespace AcuPay.Domain.Services;

public interface IInvoiceService
{
    Task<PagedResult<Invoice>> GetPageAsync(string? status, string? vendorCode, int page, CancellationToken ct);
    Task<Invoice> GetAsync(int id, CancellationToken ct);
    Task<Invoice> CreateAsync(NewInvoice invoice, string createdBy, CancellationToken ct);
    Task<Invoice> MatchAsync(int id, string performedBy, CancellationToken ct);
    Task<Invoice> ApproveAsync(int id, string username, string role, string? reason, CancellationToken ct);
    Task<Invoice> RejectAsync(int id, string username, string role, string? reason, CancellationToken ct);
    Task<IReadOnlyList<SpendByVendor>> GetSpendByVendorAsync(CancellationToken ct);
}

public sealed class InvoiceService(
    IInvoiceRepository repository,
    MatchEngine engine,
    ApprovalPolicy policy,
    AcuPaySettings settings) : IInvoiceService
{
    public Task<PagedResult<Invoice>> GetPageAsync(string? status, string? vendorCode, int page, CancellationToken ct)
    {
        string? normalizedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            normalizedStatus = InvoiceStatus.All.FirstOrDefault(s => s.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new AppException(ErrorCodes.ValidationError,
                    $"Status must be one of: {string.Join(", ", InvoiceStatus.All)}.", 400);
        }

        var vendor = string.IsNullOrWhiteSpace(vendorCode) ? null : vendorCode.Trim();
        return repository.GetPageAsync(normalizedStatus, vendor, page < 1 ? 1 : page, settings.PageSize, ct);
    }

    public async Task<Invoice> GetAsync(int id, CancellationToken ct) =>
        await repository.GetByIdAsync(id, ct)
        ?? throw new AppException(ErrorCodes.InvoiceNotFound, $"Invoice {id} was not found.", 404);

    public async Task<Invoice> CreateAsync(NewInvoice invoice, string createdBy, CancellationToken ct)
    {
        var id = await repository.InsertAsync(invoice, createdBy, ct);
        return await GetAsync(id, ct);
    }

    public async Task<Invoice> MatchAsync(int id, string performedBy, CancellationToken ct)
    {
        var invoice = await GetAsync(id, ct);

        if (InvoiceStatus.IsTerminal(invoice.Status))
            throw new AppException(ErrorCodes.InvalidState, $"Invoice is already {invoice.Status} and cannot be matched again.", 409);

        var outcome = engine.Evaluate(invoice);
        await repository.UpdateMatchAsync(id, outcome.Status, outcome.Note,
            outcome.AutoApproved ? SystemUser.Name : null, performedBy, ct);

        return await GetAsync(id, ct);
    }

    public async Task<Invoice> ApproveAsync(int id, string username, string role, string? reason, CancellationToken ct)
    {
        var invoice = await GetAsync(id, ct);
        policy.EnsureCanApprove(invoice, username, role, reason);
        await repository.DecideAsync(id, InvoiceStatus.Approved, username, Clean(reason), ct);
        return await GetAsync(id, ct);
    }

    public async Task<Invoice> RejectAsync(int id, string username, string role, string? reason, CancellationToken ct)
    {
        var invoice = await GetAsync(id, ct);
        policy.EnsureCanReject(invoice, username, role, reason);
        await repository.DecideAsync(id, InvoiceStatus.Rejected, username, Clean(reason), ct);
        return await GetAsync(id, ct);
    }

    public Task<IReadOnlyList<SpendByVendor>> GetSpendByVendorAsync(CancellationToken ct) =>
        repository.GetSpendByVendorAsync(ct);

    private static string? Clean(string? reason) => string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
}
