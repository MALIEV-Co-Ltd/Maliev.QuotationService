namespace Maliev.QuotationService.Api.DTOs.Responses;

/// <summary>
/// Response model for an internal note.
/// </summary>
public class InternalNoteResponse
{
    /// <summary>
    /// The unique identifier of the note.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user ID of the note author.
    /// </summary>
    public string AuthorUserId { get; set; } = string.Empty;

    /// <summary>
    /// The content of the note.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when the note was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
