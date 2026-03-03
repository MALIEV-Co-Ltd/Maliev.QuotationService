using Maliev.QuotationService.Api.Services;
using Maliev.QuotationService.Domain.Enums;
using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Api.Services.Metrics;
using Microsoft.Extensions.Logging;
using Moq;
using MassTransit;
using Xunit;

namespace Maliev.QuotationService.Tests.Unit.Services;

public class QuotationServiceMockTests
{
    private readonly Mock<ILogger<Api.Services.QuotationService>> _mockLogger;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly MetricsService _metricsService;

    public QuotationServiceMockTests()
    {
        _mockLogger = new Mock<ILogger<Api.Services.QuotationService>>();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _metricsService = new MetricsService();
    }

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
    [InlineData(QuotationStatus.Cancelled, QuotationStatus.Draft, false)]
    [InlineData(QuotationStatus.Cancelled, QuotationStatus.PendingApproval, false)]
    [InlineData(QuotationStatus.Expired, QuotationStatus.Accepted, false)]
    [InlineData(QuotationStatus.Accepted, QuotationStatus.Cancelled, false)]
    public void QuotationStateMachine_IsValidTransition_ReturnsExpectedResult(
        QuotationStatus from, 
        QuotationStatus to, 
        bool expected)
    {
        var result = QuotationStateMachine.IsValidTransition(from, to);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void QuotationStateMachine_GetValidTransitions_Draft_ReturnsExpected()
    {
        var transitions = QuotationStateMachine.GetValidTransitions(QuotationStatus.Draft);
        
        Assert.Contains(QuotationStatus.PendingApproval, transitions);
        Assert.Contains(QuotationStatus.CustomerReview, transitions);
        Assert.Contains(QuotationStatus.Cancelled, transitions);
    }

    [Fact]
    public void QuotationStateMachine_GetValidTransitions_PendingApproval_ReturnsExpected()
    {
        var transitions = QuotationStateMachine.GetValidTransitions(QuotationStatus.PendingApproval);
        
        Assert.Contains(QuotationStatus.Approved, transitions);
        Assert.Contains(QuotationStatus.Draft, transitions);
        Assert.Contains(QuotationStatus.Cancelled, transitions);
    }

    [Fact]
    public void QuotationStateMachine_GetValidTransitions_Approved_ReturnsExpected()
    {
        var transitions = QuotationStateMachine.GetValidTransitions(QuotationStatus.Approved);
        
        Assert.Contains(QuotationStatus.CustomerReview, transitions);
        Assert.Contains(QuotationStatus.Cancelled, transitions);
    }

    [Fact]
    public void QuotationStateMachine_GetValidTransitions_CustomerReview_ReturnsExpected()
    {
        var transitions = QuotationStateMachine.GetValidTransitions(QuotationStatus.CustomerReview);
        
        Assert.Contains(QuotationStatus.Accepted, transitions);
        Assert.Contains(QuotationStatus.Cancelled, transitions);
    }

    [Fact]
    public void QuotationStateMachine_GetValidTransitions_Accepted_ReturnsExpected()
    {
        var transitions = QuotationStateMachine.GetValidTransitions(QuotationStatus.Accepted);
        
        Assert.Contains(QuotationStatus.Expired, transitions);
    }
}
