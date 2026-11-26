using AcuPay.Domain;
using AcuPay.Domain.Rules;

namespace AcuPay.Tests;

public class MatchEngineTests
{
    private readonly MatchEngine _engine = new(TestData.Settings());

    [Fact]
    public void NoPo_IsException_NO_PO()
    {
        var r = _engine.Evaluate(TestData.Invoice(12000m, poId: null));
        Assert.Equal(InvoiceStatus.Exception, r.Status);
        Assert.Equal(MatchNotes.NoPo, r.Note);
        Assert.Null(r.VariancePercent);
    }

    [Fact]
    public void PoOfDifferentVendor_IsException_VENDOR_MISMATCH()
    {
        var r = _engine.Evaluate(TestData.Invoice(19000m, 20000m, vendorId: 1, poVendorId: 2));
        Assert.Equal(InvoiceStatus.Exception, r.Status);
        Assert.Equal(MatchNotes.VendorMismatch, r.Note);
    }

    [Fact]
    public void ClosedPo_IsException_PO_CLOSED()
    {
        var r = _engine.Evaluate(TestData.Invoice(70000m, 75000m, poStatus: PoStatus.Closed));
        Assert.Equal(InvoiceStatus.Exception, r.Status);
        Assert.Equal(MatchNotes.PoClosed, r.Note);
    }

    [Fact]
    public void VendorMismatch_WinsOverClosedPo()
    {
        var r = _engine.Evaluate(TestData.Invoice(poVendorId: 2, poStatus: PoStatus.Closed));
        Assert.Equal(MatchNotes.VendorMismatch, r.Note);
    }

    [Fact]
    public void FifteenPercentOver_IsException_OVER_PO_AMOUNT()
    {
        var r = _engine.Evaluate(TestData.Invoice(23000m, 20000m));
        Assert.Equal(InvoiceStatus.Exception, r.Status);
        Assert.Equal(MatchNotes.OverPoAmount, r.Note);
        Assert.Equal(15.00m, r.VariancePercent);
    }

    [Fact]
    public void JustOverTolerance_2_01Percent_IsException()
    {
        var r = _engine.Evaluate(TestData.Invoice(102010m, 100000m));
        Assert.Equal(MatchNotes.OverPoAmount, r.Note);
        Assert.Equal(2.01m, r.VariancePercent);
    }

    [Fact]
    public void ExactlyPlusTolerance_IsMatched_AndWaitsWhenAboveLimit()
    {
        var r = _engine.Evaluate(TestData.Invoice(102000m, 100000m));
        Assert.Equal(InvoiceStatus.Matched, r.Status);
        Assert.Equal(MatchNotes.WithinTolerance, r.Note);
        Assert.False(r.AutoApproved);
    }

    [Fact]
    public void VarianceIsRoundedBeforeComparison()
    {
        // 2.004% rounds to 2.00 -> still within tolerance
        var r = _engine.Evaluate(TestData.Invoice(102004m, 100000m));
        Assert.Equal(2.00m, r.VariancePercent);
        Assert.Equal(InvoiceStatus.Matched, r.Status);
    }

    [Fact]
    public void ExactlyMinusTolerance_MatchesAndAutoApproves_INV5002()
    {
        var r = _engine.Evaluate(TestData.Invoice(49000m, 50000m));
        Assert.Equal(-2.00m, r.VariancePercent);
        Assert.Equal(InvoiceStatus.Approved, r.Status);
        Assert.Equal(MatchNotes.AutoApproved, r.Note);
        Assert.True(r.AutoApproved);
    }

    [Fact]
    public void UnderBilled10Percent_IsMatchedAndAutoApproved_INV5007()
    {
        var r = _engine.Evaluate(TestData.Invoice(36000m, 40000m));
        Assert.Equal(-10.00m, r.VariancePercent);
        Assert.Equal(InvoiceStatus.Approved, r.Status);
    }

    [Fact]
    public void UnderBilledAboveLimit_StaysMatched_UNDER_PO_AMOUNT()
    {
        var r = _engine.Evaluate(TestData.Invoice(60000m, 70000m));
        Assert.Equal(InvoiceStatus.Matched, r.Status);
        Assert.Equal(MatchNotes.UnderPoAmount, r.Note);
    }

    [Fact]
    public void MatchedAboveLimit_WaitsForHuman_INV5001()
    {
        var r = _engine.Evaluate(TestData.Invoice(101500m, 100000m));
        Assert.Equal(1.50m, r.VariancePercent);
        Assert.Equal(InvoiceStatus.Matched, r.Status);
        Assert.Equal(MatchNotes.WithinTolerance, r.Note);
    }

    [Fact]
    public void WithinTolerance_AtExactlyTheLimit_IsAutoApproved()
    {
        var r = _engine.Evaluate(TestData.Invoice(50000m, 50000m));
        Assert.Equal(InvoiceStatus.Approved, r.Status);
    }

    [Fact]
    public void WithinTolerance_OneCentOverTheLimit_IsNotAutoApproved()
    {
        var r = _engine.Evaluate(TestData.Invoice(50000.01m, 50000.01m));
        Assert.Equal(InvoiceStatus.Matched, r.Status);
    }

    [Fact]
    public void ExceptionIsNeverAutoApproved_EvenWhenSmall()
    {
        var r = _engine.Evaluate(TestData.Invoice(1m, 100m, poId: null));
        Assert.Equal(InvoiceStatus.Exception, r.Status);
        Assert.False(r.AutoApproved);
    }

    [Fact]
    public void VarianceFor_IsNullWhenNotCalculated()
    {
        Assert.Null(MatchEngine.VarianceFor(TestData.Invoice(poId: null)));
        Assert.Null(MatchEngine.VarianceFor(TestData.Invoice(poStatus: PoStatus.Closed)));
        Assert.Null(MatchEngine.VarianceFor(TestData.Invoice(poVendorId: 2)));
        Assert.Equal(0.50m, MatchEngine.VarianceFor(TestData.Invoice(20100m, 20000m)));
    }
}
