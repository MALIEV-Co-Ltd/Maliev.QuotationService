using Maliev.QuotationService.Api.Services;
using Maliev.QuotationService.Domain.Enums;
using Maliev.QuotationService.Api.Services.Metrics;
using Microsoft.Extensions.Logging;
using Moq;
using MassTransit;
using Xunit;

namespace Maliev.QuotationService.Tests.Unit.Services;

public class RfqServiceMockTests
{
    private readonly Mock<ILogger<RfqService>> _mockLogger;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly MetricsService _metricsService;

    public RfqServiceMockTests()
    {
        _mockLogger = new Mock<ILogger<RfqService>>();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _metricsService = new MetricsService();
    }

    [Fact]
    public void MetricsService_RecordsRfqCreated_DoesNotThrow()
    {
        var exception = Record.Exception(() => _metricsService.RecordRfqCreated("Website"));

        Assert.Null(exception);
    }

    [Fact]
    public void MetricsService_RecordsRfqStatusTransition_DoesNotThrow()
    {
        var exception = Record.Exception(() => _metricsService.RecordRfqStatusTransition("New", "Qualified"));

        Assert.Null(exception);
    }

    [Fact]
    public void MetricsService_RecordsQuotationCreated_DoesNotThrow()
    {
        var exception = Record.Exception(() => _metricsService.RecordQuotationCreated());

        Assert.Null(exception);
    }

    [Fact]
    public void MetricsService_RecordsQuotationApproved_DoesNotThrow()
    {
        var exception = Record.Exception(() => _metricsService.RecordQuotationApproved());

        Assert.Null(exception);
    }

    [Fact]
    public void MetricsService_RecordsInternalNoteCreated_DoesNotThrow()
    {
        var exception = Record.Exception(() => _metricsService.RecordInternalNoteCreated());

        Assert.Null(exception);
    }

    [Fact]
    public void MetricsService_RecordsQuotationVersionCreated_DoesNotThrow()
    {
        var exception = Record.Exception(() => _metricsService.RecordQuotationVersionCreated());

        Assert.Null(exception);
    }

    [Fact]
    public void MetricsService_RecordsQuotationStatusTransition_DoesNotThrow()
    {
        var exception = Record.Exception(() => _metricsService.RecordQuotationStatusTransition("Draft", "PendingApproval"));

        Assert.Null(exception);
    }

    [Fact]
    public void MetricsService_Dispose_DoesNotThrow()
    {
        var metrics = new MetricsService();

        var exception = Record.Exception(() => metrics.Dispose());

        Assert.Null(exception);
    }
}
