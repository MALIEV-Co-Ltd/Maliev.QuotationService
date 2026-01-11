using Maliev.QuotationService.Data.Entities;
using Maliev.QuotationService.Data.Enums;

namespace Maliev.QuotationService.Api.Services.Interfaces;

/// <summary>
/// Service for managing Requests for Quotation (RFQs).
/// </summary>
public interface IRfqService
{
    /// <summary>
    /// Creates a new RFQ, optionally creating a new customer if they don't exist.
    /// </summary>
    Task<Rfq> CreateAsync(
        string customerEmail,
        string customerName,
        string? customerPhoneNumber,
        RfqChannel channelSource,
        object requestDetails,
        List<Guid>? uploadServiceFileIds,
        string currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an RFQ by its unique identifier.
    /// </summary>
    Task<Rfq?> GetByIdAsync(
        Guid rfqId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all RFQs with optional filtering.
    /// </summary>
    Task<(List<Rfq> Rfqs, int TotalCount)> GetAllAsync(
        RfqChannel? channelSource = null,
        RfqStatus? status = null,
        Guid? customerId = null,
        string? assignedStaffUserId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing RFQ's details.
    /// </summary>
    Task<Rfq> UpdateAsync(
        Guid rfqId,
        object? requestDetails,
        string? assignedStaffUserId,
        string currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of an RFQ.
    /// </summary>
    Task<Rfq> UpdateStatusAsync(
        Guid rfqId,
        RfqStatus status,
        string currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an internal note to an RFQ.
    /// </summary>
    Task<InternalNote> AddNoteAsync(
        Guid rfqId,
        string content,
        string currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns an RFQ to a staff member.
    /// </summary>
    Task<Rfq> AssignAsync(
        Guid rfqId,
        string assignedStaffUserId,
        string currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an RFQ as converted.
    /// </summary>
    Task<Guid> MarkRfqAsConvertedAsync(
        Guid rfqId,
        string currentUserId,
        CancellationToken cancellationToken = default);
}
