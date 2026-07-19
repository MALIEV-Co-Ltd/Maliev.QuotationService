namespace Maliev.QuotationService.Api.ExternalClients.Interfaces;

/// <summary>
/// Client interface for interacting with the Upload Service API.
/// </summary>
public interface IUploadServiceClient
{
    /// <summary>
    /// Validates a file reference and retrieves its metadata.
    /// </summary>
    /// <param name="uploadServiceFileId">The unique identifier of the file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file metadata if found, otherwise null.</returns>
    Task<FileMetadataDto?> ValidateFileReferenceAsync(Guid uploadServiceFileId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents file metadata from the Upload Service.
/// </summary>
public record FileMetadataDto(
    Guid Id,
    string FileName,
    string FileType,
    long FileSizeBytes,
    DateTime UploadedAt);
