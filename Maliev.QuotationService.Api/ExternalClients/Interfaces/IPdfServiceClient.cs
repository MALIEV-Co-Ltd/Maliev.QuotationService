namespace Maliev.QuotationService.Api.ExternalClients.Interfaces;

public interface IPdfServiceClient
{
    Task<PdfGenerationResponseDto> GeneratePdfAsync(QuotationPdfPayload payload, CancellationToken cancellationToken = default);
}

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

public record LineItemPayload(
    int LineNumber,
    string MaterialName,
    decimal Quantity,
    string QuantityUnit,
    decimal UnitPrice,
    decimal LineTotal);

public record PdfGenerationResponseDto(
    Guid PdfServiceFileId,
    string FileName,
    DateTime GeneratedAt);
