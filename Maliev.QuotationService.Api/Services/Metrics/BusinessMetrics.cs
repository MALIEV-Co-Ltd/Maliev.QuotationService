using Prometheus;

namespace Maliev.QuotationService.Api.Services.Metrics;

public static class BusinessMetrics
{
    // RFQ metrics
    public static readonly Counter RfqCreatedTotal = Prometheus.Metrics.CreateCounter(
        "quotation_service_rfq_created_total",
        "Total number of RFQs created",
        new CounterConfiguration
        {
            LabelNames = new[] { "channel" }
        });

    public static readonly Counter RfqStatusTransitionsTotal = Prometheus.Metrics.CreateCounter(
        "quotation_service_rfq_status_transitions_total",
        "Total number of RFQ status transitions",
        new CounterConfiguration
        {
            LabelNames = new[] { "from_status", "to_status" }
        });

    // Quotation metrics
    public static readonly Counter QuotationCreatedTotal = Prometheus.Metrics.CreateCounter(
        "quotation_service_quotation_created_total",
        "Total number of quotations created");

    public static readonly Counter QuotationStatusTransitionsTotal = Prometheus.Metrics.CreateCounter(
        "quotation_service_quotation_status_transitions_total",
        "Total number of quotation status transitions",
        new CounterConfiguration
        {
            LabelNames = new[] { "from_status", "to_status" }
        });

    public static readonly Counter QuotationVersionsCreatedTotal = Prometheus.Metrics.CreateCounter(
        "quotation_service_quotation_versions_created_total",
        "Total number of quotation versions created");

    public static readonly Counter QuotationApprovalsTotal = Prometheus.Metrics.CreateCounter(
        "quotation_service_quotation_approvals_total",
        "Total number of quotation approvals");

    // Customer metrics
    public static readonly Counter CustomerMatchesSuggestedTotal = Prometheus.Metrics.CreateCounter(
        "quotation_service_customer_matches_suggested_total",
        "Total number of customer matches suggested",
        new CounterConfiguration
        {
            LabelNames = new[] { "match_count" }
        });

    public static readonly Counter CustomerLinksCreatedTotal = Prometheus.Metrics.CreateCounter(
        "quotation_service_customer_links_created_total",
        "Total number of customer links created");

    // External service metrics
    public static readonly Counter ExternalServiceCacheHitsTotal = Prometheus.Metrics.CreateCounter(
        "quotation_service_external_service_cache_hits_total",
        "Total number of external service cache hits",
        new CounterConfiguration
        {
            LabelNames = new[] { "service" }
        });

    public static readonly Counter ExternalServiceCacheMissesTotal = Prometheus.Metrics.CreateCounter(
        "quotation_service_external_service_cache_misses_total",
        "Total number of external service cache misses",
        new CounterConfiguration
        {
            LabelNames = new[] { "service" }
        });

    public static readonly Histogram ExternalServiceResponseTime = Prometheus.Metrics.CreateHistogram(
        "quotation_service_external_service_response_seconds",
        "Response time for external service calls in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "service", "operation" },
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 10) // 1ms to ~1s
        });

    // PDF generation metrics
    public static readonly Counter PdfGeneratedTotal = Prometheus.Metrics.CreateCounter(
        "quotation_service_pdf_generated_total",
        "Total number of PDFs generated");

    public static readonly Histogram PdfGenerationTime = Prometheus.Metrics.CreateHistogram(
        "quotation_service_pdf_generation_seconds",
        "Time taken to generate PDF in seconds",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.1, 2, 10) // 100ms to ~100s
        });

    // Internal notes metrics
    public static readonly Counter InternalNotesCreatedTotal = Prometheus.Metrics.CreateCounter(
        "quotation_service_internal_notes_created_total",
        "Total number of internal notes created");

    // Business KPI metrics
    public static readonly Gauge ActiveQuotationsGauge = Prometheus.Metrics.CreateGauge(
        "quotation_service_active_quotations",
        "Number of quotations in active states",
        new GaugeConfiguration
        {
            LabelNames = new[] { "status" }
        });

    public static readonly Gauge ActiveRfqsGauge = Prometheus.Metrics.CreateGauge(
        "quotation_service_active_rfqs",
        "Number of RFQs in active states",
        new GaugeConfiguration
        {
            LabelNames = new[] { "status" }
        });
}
