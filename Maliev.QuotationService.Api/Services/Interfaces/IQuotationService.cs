using Maliev.QuotationService.Data.Entities;
using Maliev.QuotationService.Data.Enums;

namespace Maliev.QuotationService.Api.Services.Interfaces;

public interface IQuotationService
{
    Task<Quotation> CreateAsync(
        Guid customerId,
        Guid? sourceRfqId,
        DateTime validityPeriodStart,
        DateTime validityPeriodEnd,
        IEnumerable<object> lineItems,
        string? deliveryExpectations,
        string currentUserId,
        object? discountStructure = null,
        CancellationToken cancellationToken = default);

    Task<Quotation?> GetByIdAsync(
        Guid quotationId,
        CancellationToken cancellationToken = default);

    Task<(List<Quotation> Quotations, int TotalCount)> GetAllAsync(
        QuotationStatus? status = null,
        Guid? customerId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<Quotation> UpdateAsync(
        Guid quotationId,
        IEnumerable<object>? lineItems,
        string changeSummary,
        string? deliveryExpectations = null,
        object? discountStructure = null,
        string? currentUserId = null,
        CancellationToken cancellationToken = default);

    Task<Quotation> UpdateStatusAsync(
        Guid quotationId,
        QuotationStatus status,
        string currentUserId,
        CancellationToken cancellationToken = default);

    Task<Quotation> ApproveAsync(
        Guid quotationId,
        string currentUserId,
        CancellationToken cancellationToken = default);

    Task<List<QuotationVersion>> GetVersionsAsync(
        Guid quotationId,
        CancellationToken cancellationToken = default);

    Task<QuotationVersion?> GetVersionByNumberAsync(
        Guid quotationId,
        int versionNumber,
        CancellationToken cancellationToken = default);

    Task<byte[]> GeneratePdfAsync(
        Guid quotationId,
        int? versionNumber = null,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid quotationId,
        CancellationToken cancellationToken = default);
}
