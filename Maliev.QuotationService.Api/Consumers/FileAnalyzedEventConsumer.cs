using Maliev.QuotationService.Data;
using Maliev.MessagingContracts.Contracts.Geometry;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Maliev.QuotationService.Api.Consumers;

/// <summary>
/// Consumes FileAnalyzedEvent to update file references with geometric metrics.
/// </summary>
public class FileAnalyzedEventConsumer : IConsumer<FileAnalyzedEvent>
{
    private readonly QuotationDbContext _dbContext;
    private readonly ILogger<FileAnalyzedEventConsumer> _logger;

    public FileAnalyzedEventConsumer(QuotationDbContext dbContext, ILogger<FileAnalyzedEventConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<FileAnalyzedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation("Processing FileAnalyzedEvent for FileId: {FileId}", message.Payload.FileId);

        if (Guid.TryParse(message.Payload.FileId, out var fileId))
        {
            var references = await _dbContext.FileReferences
                .Where(f => f.UploadServiceFileId == fileId)
                .ToListAsync();

            if (references.Any())
            {
                foreach (var reference in references)
                {
                    reference.VolumeCm3 = message.Payload.Metrics.VolumeCm3;
                    reference.SupportVolumeCm3 = message.Payload.Metrics.SupportVolumeCm3;
                    reference.SurfaceAreaCm2 = message.Payload.Metrics.SurfaceAreaCm2;
                    reference.IsManifold = message.Payload.Metrics.IsManifold;
                    reference.TriangleCount = message.Payload.Metrics.TriangleCount;
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Updated metrics for {Count} file references associated with FileId {FileId}.", references.Count, fileId);
            }
            else
            {
                _logger.LogWarning("No file references found for FileId {FileId}. Metrics not updated.", fileId);
            }
        }
        else
        {
            _logger.LogError("Invalid FileId format: {FileId}", message.Payload.FileId);
        }
    }
}
