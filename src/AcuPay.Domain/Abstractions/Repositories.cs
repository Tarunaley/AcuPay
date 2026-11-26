using AcuPay.Domain.Entities;

namespace AcuPay.Domain.Abstractions;

public interface IInvoiceRepository
{
    Task<PagedResult<Invoice>> GetPageAsync(string? status, string? vendorCode, int page, int pageSize, CancellationToken ct);
    Task<Invoice?> GetByIdAsync(int invoiceId, CancellationToken ct);
    Task<int> InsertAsync(NewInvoice invoice, string createdBy, CancellationToken ct);
    Task UpdateMatchAsync(int invoiceId, string status, string matchNote, string? decidedBy, string performedBy, CancellationToken ct);
    Task DecideAsync(int invoiceId, string status, string decidedBy, string? reason, CancellationToken ct);
    Task<IReadOnlyList<SpendByVendor>> GetSpendByVendorAsync(CancellationToken ct);
}

public interface IUserRepository
{
    Task<UserAccount?> GetByUsernameAsync(string username, CancellationToken ct);
}

public interface IDatabaseHealth
{
    Task<bool> CanConnectAsync(CancellationToken ct);
}
