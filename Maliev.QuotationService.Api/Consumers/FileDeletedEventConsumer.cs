using Maliev.MessagingContracts.Contracts.Uploads;
using Maliev.MessagingContracts.Generated;
using Maliev.QuotationService.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Maliev.QuotationService.Api.Consumers;

/// <summary>
/// Consumes FileDeletedEvent to clean up local file references in Quotation Service.
/// </summary>
public class FileDeletedEventConsumer : IConsumer<FileDeletedEvent>
{
    private readonly QuotationDbContext _dbContext;
    private readonly ILogger<FileDeletedEventConsumer> _logger;

    public FileDeletedEventConsumer(QuotationDbContext dbContext, ILogger<FileDeletedEventConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<FileDeletedEvent> context)
    {
        var message = context.Message;

        // Only process if it belongs to this service context or if we have a matching file ID
        if (message.Payload.ServiceId != "quotation-service")
        {
            // Even if not explicitly for this service, we check if we have a reference to this FileId
            // as some files might be shared or uploaded by other services but referenced here.
        }

        _logger.LogInformation("Processing FileDeletedEvent for FileId: {FileId}, StoragePath: {StoragePath}",
            message.Payload.FileId, message.Payload.StoragePath);

        if (Guid.TryParse(message.Payload.FileId, out var fileId))
        {
            var references = await _dbContext.FileReferences
                .Where(f => f.UploadServiceFileId == fileId)
                .ToListAsync();

            if (references.Any())
            {
                _dbContext.FileReferences.RemoveRange(references);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Removed {Count} file references associated with FileId {FileId}.", references.Count, fileId);
            }
        }
    }
}
