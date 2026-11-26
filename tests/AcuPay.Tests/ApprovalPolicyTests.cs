using AcuPay.Domain;
using AcuPay.Domain.Exceptions;
using AcuPay.Domain.Rules;

namespace AcuPay.Tests;

public class ApprovalPolicyTests
{
    private readonly ApprovalPolicy _policy = new(TestData.Settings());
    private const string TwentyChars = "Approved by contract."; // 21 chars

    [Fact]
    public void Requester_CannotApprove_FORBIDDEN_ROLE()
    {
        var ex = Assert.Throws<AppException>(() =>
            _policy.EnsureCanApprove(TestData.Invoice(status: InvoiceStatus.Matched), "requester1", AppRoles.Requester, null));
        Assert.Equal(ErrorCodes.ForbiddenRole, ex.Code);
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public void Creator_CannotDecideOwnInvoice_EvenIfStillPending()
    {
        var invoice = TestData.Invoice(status: InvoiceStatus.Pending, createdBy: "approver1");
        var ex = Assert.Throws<AppException>(() => _policy.EnsureCanApprove(invoice, "approver1", AppRoles.Approver, null));
        Assert.Equal(ErrorCodes.SelfApprovalBlocked, ex.Code);
    }

    [Fact]
    public void SelfApprovalCheck_IsCaseInsensitive()
    {
        var invoice = TestData.Invoice(status: InvoiceStatus.Matched, createdBy: "Approver1");
        var ex = Assert.Throws<AppException>(() => _policy.EnsureCanReject(invoice, "approver1", AppRoles.Approver, TwentyChars));
        Assert.Equal(ErrorCodes.SelfApprovalBlocked, ex.Code);
    }

    [Theory]
    [InlineData(InvoiceStatus.Approved)]
    [InlineData(InvoiceStatus.Rejected)]
    [InlineData(InvoiceStatus.Pending)]
    public void NonDecidableStatus_IsInvalidState_409(string status)
    {
        var ex = Assert.Throws<AppException>(() =>
            _policy.EnsureCanReject(TestData.Invoice(status: status), "approver1", AppRoles.Approver, TwentyChars));
        Assert.Equal(ErrorCodes.InvalidState, ex.Code);
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public void ExceptionInvoice_ApprovedByApprover_RequiresFinance()
    {
        var invoice = TestData.Invoice(status: InvoiceStatus.Exception);
        var ex = Assert.Throws<AppException>(() => _policy.EnsureCanApprove(invoice, "approver1", AppRoles.Approver, TwentyChars));
        Assert.Equal(ErrorCodes.OverrideRequiresFinance, ex.Code);
    }

    [Fact]
    public void ExceptionInvoice_ByFinanceWithoutReason_IsReasonRequired_400()
    {
        var invoice = TestData.Invoice(status: InvoiceStatus.Exception);
        var ex = Assert.Throws<AppException>(() => _policy.EnsureCanApprove(invoice, "finance1", AppRoles.Finance, null));
        Assert.Equal(ErrorCodes.ReasonRequired, ex.Code);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public void ExceptionInvoice_ByFinanceWithReason_IsAllowed()
    {
        var invoice = TestData.Invoice(status: InvoiceStatus.Exception);
        _policy.EnsureCanApprove(invoice, "finance1", AppRoles.Finance, TwentyChars);
    }

    [Fact]
    public void MatchedInvoice_ByApproverWithoutReason_IsAllowed()
    {
        var invoice = TestData.Invoice(status: InvoiceStatus.Matched);
        _policy.EnsureCanApprove(invoice, "approver1", AppRoles.Approver, null);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("short")]
    [InlineData("         x         ")]
    public void Reject_WithShortOrMissingReason_IsReasonRequired(string? reason)
    {
        var invoice = TestData.Invoice(status: InvoiceStatus.Matched);
        var ex = Assert.Throws<AppException>(() => _policy.EnsureCanReject(invoice, "approver1", AppRoles.Approver, reason));
        Assert.Equal(ErrorCodes.ReasonRequired, ex.Code);
    }

    [Fact]
    public void Reject_WithValidReason_IsAllowed()
    {
        var invoice = TestData.Invoice(status: InvoiceStatus.Exception);
        _policy.EnsureCanReject(invoice, "approver1", AppRoles.Approver, TwentyChars);
    }
}
