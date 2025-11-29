using Maliev.QuotationService.Data.Entities;
using Maliev.QuotationService.Data.Enums;

namespace Maliev.QuotationService.Api.Services.Interfaces;

public interface IRfqService
{
    Task<Rfq> CreateAsync(
        Guid customerId,
        RfqChannel channelSource,
        object requestDetails,
        List<Guid>? uploadServiceFileIds,
        string currentUserId,
        CancellationToken cancellationToken = default);

    Task<Rfq?> GetByIdAsync(
        Guid rfqId,
        CancellationToken cancellationToken = default);

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

    Task<Rfq> UpdateAsync(
        Guid rfqId,
        object? requestDetails,
        string? assignedStaffUserId,
        string currentUserId,
        CancellationToken cancellationToken = default);

    Task<Rfq> UpdateStatusAsync(
        Guid rfqId,
        RfqStatus status,
        string currentUserId,
        CancellationToken cancellationToken = default);

    Task<InternalNote> AddNoteAsync(
        Guid rfqId,
        string content,
        string currentUserId,
        CancellationToken cancellationToken = default);

    Task<Rfq> AssignAsync(
        Guid rfqId,
        string assignedStaffUserId,
        string currentUserId,
        CancellationToken cancellationToken = default);

    Task<Guid> ConvertToQuotationAsync(
        Guid rfqId,
        string currentUserId,
        CancellationToken cancellationToken = default);
}
