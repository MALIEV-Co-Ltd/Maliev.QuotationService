using System.Diagnostics.Metrics;

namespace Maliev.QuotationService.Api.Services.Metrics;

/// <summary>
/// Service for collecting business metrics using OpenTelemetry
/// </summary>
public class MetricsService : IDisposable
{
    private readonly Meter _meter;

    // RFQ Metrics
    private readonly Counter<long> _rfqCreated;
    private readonly Counter<long> _rfqStatusTransitions;

    // Quotation Metrics
    private readonly Counter<long> _quotationCreated;
    private readonly Counter<long> _quotationStatusTransitions;
    private readonly Counter<long> _quotationApprovals;
    private readonly Counter<long> _quotationVersionsCreated;

    // Note Metrics
    private readonly Counter<long> _internalNotesCreated;

    /// <summary>
    /// Initializes a new instance of the MetricsService.
    /// </summary>
    public MetricsService()
    {
        _meter = new Meter("quotations-meter", "1.0.0");

        _rfqCreated = _meter.CreateCounter<long>(
            "quotation_service_rfq_created_total",
            description: "Total number of RFQs created");

        _rfqStatusTransitions = _meter.CreateCounter<long>(
            "quotation_service_rfq_status_transitions_total",
            description: "Total number of RFQ status transitions");

        _quotationCreated = _meter.CreateCounter<long>(
            "quotation_service_quotation_created_total",
            description: "Total number of quotations created");

        _quotationStatusTransitions = _meter.CreateCounter<long>(
            "quotation_service_quotation_status_transitions_total",
            description: "Total number of quotation status transitions");

        _quotationApprovals = _meter.CreateCounter<long>(
            "quotation_service_quotation_approvals_total",
            description: "Total number of quotation approvals");

        _quotationVersionsCreated = _meter.CreateCounter<long>(
            "quotation_service_quotation_versions_created_total",
            description: "Total number of quotation versions created");

        _internalNotesCreated = _meter.CreateCounter<long>(
            "quotation_service_internal_notes_created_total",
            description: "Total number of internal notes created");
    }

    // RFQ Methods

    /// <summary>
    /// Records an RFQ creation event.
    /// </summary>
    /// <param name="channel">The channel source of the RFQ.</param>
    public void RecordRfqCreated(string channel)
    {
        _rfqCreated.Add(1, new KeyValuePair<string, object?>("channel", channel));
    }

    /// <summary>
    /// Records an RFQ status transition.
    /// </summary>
    /// <param name="fromStatus">The previous status.</param>
    /// <param name="toStatus">The new status.</param>
    public void RecordRfqStatusTransition(string fromStatus, string toStatus)
    {
        _rfqStatusTransitions.Add(1,
            new KeyValuePair<string, object?>("from_status", fromStatus),
            new KeyValuePair<string, object?>("to_status", toStatus));
    }

    // Quotation Methods

    /// <summary>
    /// Records a quotation creation event.
    /// </summary>
    public void RecordQuotationCreated()
    {
        _quotationCreated.Add(1);
    }

    /// <summary>
    /// Records a quotation status transition.
    /// </summary>
    /// <param name="fromStatus">The previous status.</param>
    /// <param name="toStatus">The new status.</param>
    public void RecordQuotationStatusTransition(string fromStatus, string toStatus)
    {
        _quotationStatusTransitions.Add(1,
            new KeyValuePair<string, object?>("from_status", fromStatus),
            new KeyValuePair<string, object?>("to_status", toStatus));
    }

    /// <summary>
    /// Records a quotation approval event.
    /// </summary>
    public void RecordQuotationApproved()
    {
        _quotationApprovals.Add(1);
    }

    /// <summary>
    /// Records a quotation version creation event.
    /// </summary>
    public void RecordQuotationVersionCreated()
    {
        _quotationVersionsCreated.Add(1);
    }

    // Note Methods

    /// <summary>
    /// Records an internal note creation event.
    /// </summary>
    public void RecordInternalNoteCreated()
    {
        _internalNotesCreated.Add(1);
    }

    /// <summary>
    /// Disposes of the metrics service and releases resources.
    /// </summary>
    public void Dispose()
    {
        _meter?.Dispose();
        GC.SuppressFinalize(this);
    }
}
