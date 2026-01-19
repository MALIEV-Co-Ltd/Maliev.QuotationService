using Maliev.QuotationService.Api.Services;
using Maliev.QuotationService.Data.Enums;
using Xunit;

namespace Maliev.QuotationService.Tests.Unit;

public class QuotationStateMachineTests
{
    [Theory]
    [InlineData(QuotationStatus.Draft, QuotationStatus.PendingApproval, true)]
    [InlineData(QuotationStatus.Draft, QuotationStatus.CustomerReview, true)]
    [InlineData(QuotationStatus.Draft, QuotationStatus.Cancelled, true)]
    [InlineData(QuotationStatus.PendingApproval, QuotationStatus.Approved, true)]
    [InlineData(QuotationStatus.PendingApproval, QuotationStatus.Draft, true)]
    [InlineData(QuotationStatus.Approved, QuotationStatus.CustomerReview, true)]
    [InlineData(QuotationStatus.CustomerReview, QuotationStatus.Accepted, true)]
    [InlineData(QuotationStatus.Accepted, QuotationStatus.Expired, true)]
    [InlineData(QuotationStatus.Draft, QuotationStatus.Accepted, false)]
    [InlineData(QuotationStatus.Accepted, QuotationStatus.Draft, false)]
    public void IsValidTransition_ShouldReturnExpectedResult(QuotationStatus from, QuotationStatus to, bool expected)
    {
        Assert.Equal(expected, QuotationStateMachine.IsValidTransition(from, to));
    }

    [Fact]
    public void GetValidTransitions_ShouldReturnExpectedList()
    {
        var draftTransitions = QuotationStateMachine.GetValidTransitions(QuotationStatus.Draft);
        Assert.Contains(QuotationStatus.PendingApproval, draftTransitions);
        Assert.Contains(QuotationStatus.CustomerReview, draftTransitions);
        Assert.Contains(QuotationStatus.Cancelled, draftTransitions);
    }
}
