namespace Maliev.QuotationService.Api.ExternalClients.Interfaces;

/// <summary>
/// Client interface for interacting with the PDF Service API.
/// </summary>
public interface IPdfServiceClient
{
    /// <summary>
    /// Generates a PDF for a quotation.
    /// </summary>
    /// <param name="payload">The quotation data to include in the PDF.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The PDF generation response.</returns>
    Task<PdfGenerationResponseDto> GeneratePdfAsync(QuotationPdfPayload payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Payload for generating a quotation PDF.
/// </summary>
public record QuotationPdfPayload(
    Guid QuotationId,
    int VersionNumber,
    string CustomerName,
    string? CustomerEmail,
    DateOnly ValidityPeriodStart,
    DateOnly ValidityPeriodEnd,
    List<LineItemPayload> LineItems,
    decimal TotalPrice,
    string CurrencyCode,
    string? SpecialTerms);

/// <summary>
/// Represents a line item in the PDF payload.
/// </summary>
public record LineItemPayload(
    int LineNumber,
    string MaterialName,
    decimal Quantity,
    string QuantityUnit,
    decimal UnitPrice,
    decimal LineTotal);

/// <summary>
/// Response from the PDF generation endpoint.
/// </summary>
public record PdfGenerationResponseDto(
    Guid PdfServiceFileId,
    string FileName,
    DateTime GeneratedAt);
