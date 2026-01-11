using System.Text.Json;
using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Api.Services.Metrics;
using Maliev.QuotationService.Data;
using Maliev.QuotationService.Data.Entities;
using Maliev.QuotationService.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace Maliev.QuotationService.Api.Services;

/// <summary>
/// Implementation of the RFQ management service.
/// </summary>
public class RfqService : IRfqService
{
    private readonly QuotationDbContext _context;
    private readonly ILogger<RfqService> _logger;
    private readonly MetricsService _metricsService;

    public RfqService(
        QuotationDbContext context,
        ILogger<RfqService> logger,
        MetricsService metricsService)
    {
        _context = context;
        _logger = logger;
        _metricsService = metricsService;
    }

    /// <summary>
    /// Creates a new RFQ.
    /// </summary>
    public async Task<Rfq> CreateAsync(
        string customerEmail,
        string customerName,
        string? customerPhoneNumber,
        RfqChannel channelSource,
        object requestDetails,
        List<Guid>? uploadServiceFileIds,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing RFQ creation for {Email} via {Channel}", customerEmail, channelSource);

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. Find or create customer atomically
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == customerEmail, cancellationToken);

                if (customer == null)
                {
                    customer = new Customer
                    {
                        Id = Guid.NewGuid(),
                        Email = customerEmail,
                        Name = customerName,
                        PhoneNumber = customerPhoneNumber,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Created new customer {CustomerId} for RFQ", customer.Id);
                }

                // 2. Create RFQ
                var rfq = new Rfq
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    ChannelSource = channelSource,
                    Status = RfqStatus.New,
                    RequestDetails = JsonDocument.Parse(JsonSerializer.Serialize(requestDetails)),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Rfqs.Add(rfq);

                // 3. Create audit log entry
                var auditEntry = new AuditLogEntry
                {
                    Id = Guid.NewGuid(),
                    EntityType = AuditEntityType.RFQ,
                    EntityId = rfq.Id,
                    UserId = currentUserId,
                    ActionType = AuditActionType.Create,
                    Timestamp = DateTime.UtcNow,
                    ChangedFields = JsonDocument.Parse(JsonSerializer.Serialize(new
                    {
                        CustomerId = customer.Id,
                        ChannelSource = channelSource.ToString(),
                        Status = RfqStatus.New.ToString()
                    }))
                };

                _context.AuditLogEntries.Add(auditEntry);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // Emit metric
                _metricsService.RecordRfqCreated(channelSource.ToString());

                _logger.LogInformation("Created RFQ {RfqId} for customer {CustomerId}", rfq.Id, customer.Id);

                return rfq;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create RFQ for customer {Email}", customerEmail);
                throw;
            }
        });
    }

    /// <summary>
    /// Retrieves an RFQ by its unique identifier.
    /// </summary>
    public async Task<Rfq?> GetByIdAsync(Guid rfqId, CancellationToken cancellationToken = default)
    {
        return await _context.Rfqs
            .Include(r => r.Customer)
            .Include(r => r.FileReferences)
            .Include(r => r.InternalNotes)
            .FirstOrDefaultAsync(r => r.Id == rfqId, cancellationToken);
    }

    /// <summary>
    /// Retrieves all RFQs with optional filtering.
    /// </summary>
    public async Task<(List<Rfq> Rfqs, int TotalCount)> GetAllAsync(
        RfqChannel? channelSource = null,
        RfqStatus? status = null,
        Guid? customerId = null,
        string? assignedStaffUserId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Rfqs
            .Include(r => r.Customer)
            .AsQueryable();

        // Apply filters
        if (channelSource.HasValue)
        {
            query = query.Where(r => r.ChannelSource == channelSource.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(r => r.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(assignedStaffUserId))
        {
            query = query.Where(r => r.AssignedStaffUserId == assignedStaffUserId);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var rfqs = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (rfqs, totalCount);
    }

    /// <summary>
    /// Updates an RFQ.
    /// </summary>
    public async Task<Rfq> UpdateAsync(
        Guid rfqId,
        object? requestDetails,
        string? assignedStaffUserId,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var rfq = await _context.Rfqs.FindAsync(new object[] { rfqId }, cancellationToken);
        if (rfq == null)
        {
            throw new KeyNotFoundException($"RFQ with ID {rfqId} not found");
        }

        var changedFields = new Dictionary<string, object?>();

        if (requestDetails != null)
        {
            rfq.RequestDetails = JsonDocument.Parse(JsonSerializer.Serialize(requestDetails));
            changedFields["RequestDetails"] = requestDetails;
        }

        if (assignedStaffUserId != null && rfq.AssignedStaffUserId != assignedStaffUserId)
        {
            changedFields["AssignedStaffUserId"] = new { Old = rfq.AssignedStaffUserId, New = assignedStaffUserId };
            rfq.AssignedStaffUserId = assignedStaffUserId;
        }

        rfq.UpdatedAt = DateTime.UtcNow;

        // Create audit log entry
        if (changedFields.Count > 0)
        {
            var auditEntry = new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                EntityType = AuditEntityType.RFQ,
                EntityId = rfq.Id,
                UserId = currentUserId,
                ActionType = AuditActionType.Update,
                Timestamp = DateTime.UtcNow,
                ChangedFields = JsonDocument.Parse(JsonSerializer.Serialize(changedFields))
            };

            _context.AuditLogEntries.Add(auditEntry);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated RFQ {RfqId}", rfqId);

        return rfq;
    }

    /// <summary>
    /// Updates the status of an RFQ.
    /// </summary>
    public async Task<Rfq> UpdateStatusAsync(
        Guid rfqId,
        RfqStatus status,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var rfq = await _context.Rfqs.FindAsync(new object[] { rfqId }, cancellationToken);
        if (rfq == null)
        {
            throw new KeyNotFoundException($"RFQ with ID {rfqId} not found");
        }

        var oldStatus = rfq.Status;

        // Validate status transition
        if (!IsValidStatusTransition(oldStatus, status))
        {
            throw new InvalidOperationException($"Invalid status transition from {oldStatus} to {status}");
        }

        rfq.Status = status;
        rfq.UpdatedAt = DateTime.UtcNow;

        // Create audit log entry
        var auditEntry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = AuditEntityType.RFQ,
            EntityId = rfq.Id,
            UserId = currentUserId,
            ActionType = AuditActionType.Update,
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                Status = new { Old = oldStatus.ToString(), New = status.ToString() }
            }))
        };

        _context.AuditLogEntries.Add(auditEntry);

        await _context.SaveChangesAsync(cancellationToken);

        // Emit metric
        _metricsService.RecordRfqStatusTransition(oldStatus.ToString(), status.ToString());

        _logger.LogInformation("Updated RFQ {RfqId} status from {OldStatus} to {NewStatus}", rfqId, oldStatus, status);

        return rfq;
    }

    /// <summary>
    /// Adds an internal note to an RFQ.
    /// </summary>
    public async Task<InternalNote> AddNoteAsync(
        Guid rfqId,
        string content,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var rfqExists = await _context.Rfqs.AnyAsync(r => r.Id == rfqId, cancellationToken);
        if (!rfqExists)
        {
            throw new KeyNotFoundException($"RFQ with ID {rfqId} not found");
        }

        var note = new InternalNote
        {
            Id = Guid.NewGuid(),
            RfqId = rfqId,
            AuthorUserId = currentUserId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        _context.InternalNotes.Add(note);

        // Create audit log entry
        var auditEntry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = AuditEntityType.RFQ,
            EntityId = rfqId,
            UserId = currentUserId,
            ActionType = AuditActionType.NoteAdded,
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                NoteAdded = new { Content = content.Length > 50 ? content[..50] + "..." : content }
            }))
        };

        _context.AuditLogEntries.Add(auditEntry);

        await _context.SaveChangesAsync(cancellationToken);

        // Emit metric
        _metricsService.RecordInternalNoteCreated();

        return note;
    }

    /// <summary>
    /// Assigns an RFQ to a staff member.
    /// </summary>
    public async Task<Rfq> AssignAsync(
        Guid rfqId,
        string assignedStaffUserId,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var rfq = await _context.Rfqs.FindAsync(new object[] { rfqId }, cancellationToken);
        if (rfq == null)
        {
            throw new KeyNotFoundException($"RFQ with ID {rfqId} not found");
        }

        // In a real implementation, we would validate assignedStaffUserId against IAMService
        var oldAssignee = rfq.AssignedStaffUserId;
        rfq.AssignedStaffUserId = assignedStaffUserId;
        rfq.UpdatedAt = DateTime.UtcNow;

        // Update status to InProgress if it was New
        if (rfq.Status == RfqStatus.New)
        {
            rfq.Status = RfqStatus.InProgress;
        }

        // Create audit log entry
        var auditEntry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = AuditEntityType.RFQ,
            EntityId = rfq.Id,
            UserId = currentUserId,
            ActionType = AuditActionType.Update,
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                AssignedStaffUserId = new { Old = oldAssignee, New = assignedStaffUserId },
                Status = rfq.Status == RfqStatus.InProgress ? "New -> InProgress" : null
            }))
        };

        _context.AuditLogEntries.Add(auditEntry);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assigned RFQ {RfqId} to staff user {StaffUserId}", rfqId, assignedStaffUserId);

        return rfq;
    }

    /// <summary>
    /// Converts an RFQ into a new quotation.
    /// </summary>
    public async Task<Guid> ConvertToQuotationAsync(
        Guid rfqId,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var rfq = await _context.Rfqs
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == rfqId, cancellationToken);

        if (rfq == null)
        {
            throw new KeyNotFoundException($"RFQ with ID {rfqId} not found");
        }

        // In a real implementation, this would likely take more parameters from the UI
        // such as line items, pricing, etc. for the initial draft.
        _logger.LogInformation("Converting RFQ {RfqId} to quotation for customer {CustomerId}", rfqId, rfq.CustomerId);

        rfq.Status = RfqStatus.Converted;
        rfq.UpdatedAt = DateTime.UtcNow;

        // The actual Quotation creation is usually a separate POST /quotations call
        // that references this rfqId, but we mark it here to satisfy the workflow.
        await _context.SaveChangesAsync(cancellationToken);

        return rfq.Id;
    }

    private static bool IsValidStatusTransition(RfqStatus from, RfqStatus to)
    {
        // Define valid status transitions
        return (from, to) switch
        {
            (RfqStatus.New, RfqStatus.InProgress) => true,
            (RfqStatus.New, RfqStatus.Abandoned) => true,
            (RfqStatus.InProgress, RfqStatus.Qualified) => true,
            (RfqStatus.InProgress, RfqStatus.Converted) => true,
            (RfqStatus.InProgress, RfqStatus.Abandoned) => true,
            (RfqStatus.Qualified, RfqStatus.Converted) => true,
            (RfqStatus.Qualified, RfqStatus.Abandoned) => true,
            _ => from == to // Allow same status
        };
    }
}
