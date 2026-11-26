using AcuPay.Domain.Entities;
using AcuPay.Domain.Exceptions;
using AcuPay.Domain.Settings;

namespace AcuPay.Domain.Rules;

/// <summary>Section 5.3 approve / reject rules. Throws AppException with the exact codes from the brief.</summary>
public sealed class ApprovalPolicy(AcuPaySettings settings)
{
    public void EnsureCanApprove(Invoice invoice, string username, string role, string? reason)
    {
        EnsureCommon(invoice, username, role);

        if (invoice.Status == InvoiceStatus.Exception)
        {
            if (role != AppRoles.Finance)
                throw new AppException(ErrorCodes.OverrideRequiresFinance, "Only Finance can approve an exception invoice.", 403);
            if (!HasValidReason(reason))
                throw ReasonRequired();
        }
    }

    public void EnsureCanReject(Invoice invoice, string username, string role, string? reason)
    {
        EnsureCommon(invoice, username, role);

        if (!HasValidReason(reason))
            throw ReasonRequired();
    }

    private void EnsureCommon(Invoice invoice, string username, string role)
    {
        if (role is not (AppRoles.Approver or AppRoles.Finance))
            throw new AppException(ErrorCodes.ForbiddenRole, "Your role is not allowed to decide invoices.", 403);

        if (string.Equals(invoice.CreatedBy, username, StringComparison.OrdinalIgnoreCase))
            throw new AppException(ErrorCodes.SelfApprovalBlocked, "You cannot decide an invoice you created.", 403);

        if (!InvoiceStatus.IsDecidable(invoice.Status))
            throw new AppException(ErrorCodes.InvalidState,
                $"An invoice that is {invoice.Status} cannot be approved or rejected. Only Matched and Exception invoices can be decided.", 409);
    }

    private bool HasValidReason(string? reason) => (reason?.Trim().Length ?? 0) >= settings.MinReasonLength;

    private AppException ReasonRequired() =>
        new(ErrorCodes.ReasonRequired, $"A reason of at least {settings.MinReasonLength} characters is required.", 400);
}
