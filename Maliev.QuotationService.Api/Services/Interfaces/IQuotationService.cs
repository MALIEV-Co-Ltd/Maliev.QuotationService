using Maliev.QuotationService.Api.DTOs.Requests;
using Maliev.QuotationService.Domain.Entities;
using Maliev.QuotationService.Domain.Enums;

namespace Maliev.QuotationService.Api.Services.Interfaces;

/// <summary>
/// Service for managing quotations and their lifecycle.
/// </summary>
public interface IQuotationService
{
    /// <summary>
    /// Creates a new quotation.
    /// </summary>
    Task<Quotation> CreateAsync(
        Guid customerId,
        Guid? sourceRfqId,
        DateTime validityPeriodStart,
        DateTime validityPeriodEnd,
        IEnumerable<QuotationLineItemDto> lineItems,
        string? deliveryExpectations,
        string currentUserId,
        BillingIdentityType billingIdentityType = BillingIdentityType.Corporate,
        DiscountStructureDto? discountStructure = null,
        decimal manualDiscountAmount = 0m,
        decimal shippingCost = 0m,
        decimal taxAmount = 0m,
        string? specialTerms = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a quotation by its unique identifier.
    /// </summary>
    Task<Quotation?> GetByIdAsync(
        Guid quotationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all quotations with optional filtering.
    /// </summary>
    Task<(List<Quotation> Quotations, int TotalCount)> GetAllAsync(
        QuotationStatus? status = null,
        Guid? customerId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing quotation by creating a new version.
    /// </summary>
    Task<Quotation> UpdateAsync(
        Guid quotationId,
        IEnumerable<QuotationLineItemDto>? lineItems,
        string changeSummary,
        string? deliveryExpectations = null,
        DiscountStructureDto? discountStructure = null,
        decimal manualDiscountAmount = 0m,
        decimal shippingCost = 0m,
        decimal taxAmount = 0m,
        string? specialTerms = null,
        string? currentUserId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a quotation with transition validation.
    /// </summary>
    Task<Quotation> UpdateStatusAsync(
        Guid quotationId,
        QuotationStatus status,
        string currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a quotation (Manager role required).
    /// </summary>
    Task<Quotation> ApproveAsync(
        Guid quotationId,
        string currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an internal note to a quotation.
    /// </summary>
    Task<InternalNote> AddNoteAsync(
        Guid quotationId,
        string content,
        string currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all versions of a quotation.
    /// </summary>
    Task<List<QuotationVersion>> GetVersionsAsync(
        Guid quotationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific version of a quotation.
    /// </summary>
    Task<QuotationVersion?> GetVersionByNumberAsync(
        Guid quotationId,
        int versionNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a PDF document for a quotation.
    /// </summary>
    Task<byte[]> GeneratePdfAsync(
        Guid quotationId,
        int? versionNumber = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a quotation.
    /// </summary>
    Task DeleteAsync(
        Guid quotationId,
        CancellationToken cancellationToken = default);
}
