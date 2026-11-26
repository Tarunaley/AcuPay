namespace AcuPay.Domain;

public static class InvoiceStatus
{
    public const string Pending = "Pending";
    public const string Matched = "Matched";
    public const string Exception = "Exception";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public static readonly string[] All = [Pending, Matched, Exception, Approved, Rejected];

    public static bool IsTerminal(string status) => status is Approved or Rejected;
    public static bool IsDecidable(string status) => status is Matched or Exception;
}

public static class PoStatus
{
    public const string Open = "Open";
    public const string Closed = "Closed";
}

public static class AppRoles
{
    public const string Requester = "Requester";
    public const string Approver = "Approver";
    public const string Finance = "Finance";
}

public static class MatchNotes
{
    public const string NoPo = "NO_PO";
    public const string VendorMismatch = "VENDOR_MISMATCH";
    public const string PoClosed = "PO_CLOSED";
    public const string OverPoAmount = "OVER_PO_AMOUNT";
    public const string WithinTolerance = "WITHIN_TOLERANCE";
    public const string UnderPoAmount = "UNDER_PO_AMOUNT";
    public const string AutoApproved = "AUTO_APPROVED";
}

public static class ErrorCodes
{
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string ForbiddenRole = "FORBIDDEN_ROLE";
    public const string SelfApprovalBlocked = "SELF_APPROVAL_BLOCKED";
    public const string InvalidState = "INVALID_STATE";
    public const string OverrideRequiresFinance = "OVERRIDE_REQUIRES_FINANCE";
    public const string ReasonRequired = "REASON_REQUIRED";
    public const string DuplicateInvoice = "DUPLICATE_INVOICE";
    public const string InvoiceNotFound = "INVOICE_NOT_FOUND";
    public const string VendorNotFound = "VENDOR_NOT_FOUND";
    public const string VendorInactive = "VENDOR_INACTIVE";
    public const string PoNotFound = "PO_NOT_FOUND";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string InternalError = "INTERNAL_ERROR";
}

public static class SystemUser
{
    public const string Name = "SYSTEM";
}
