namespace Maliev.QuotationService.Api.ExternalClients.Interfaces;

public interface IUploadServiceClient
{
    Task<FileMetadataDto?> ValidateFileReferenceAsync(Guid uploadServiceFileId, CancellationToken cancellationToken = default);
}

public record FileMetadataDto(
    Guid Id,
    string FileName,
    string FileType,
    long FileSizeBytes,
    DateTime UploadedAt);
