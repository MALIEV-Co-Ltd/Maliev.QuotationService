namespace Maliev.QuotationService.Api.DTOs.Responses;

public class InternalNoteResponse
{
    public Guid Id { get; set; }
    public string AuthorUserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
