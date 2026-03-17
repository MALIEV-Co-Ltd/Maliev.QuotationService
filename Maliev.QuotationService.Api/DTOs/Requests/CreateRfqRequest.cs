using Maliev.QuotationService.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Maliev.QuotationService.Api.DTOs.Requests;

/// <summary>
/// Request model for creating a new RFQ.
/// </summary>
public class CreateRfqRequest
{
    /// <summary>
    /// The customer's email address.
    /// </summary>
    [Required]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;

    /// <summary>
    /// The customer's name.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// The customer's phone number.
    /// </summary>
    [Phone]
    public string? CustomerPhoneNumber { get; set; }

    /// <summary>
    /// The source channel where the RFQ originated.
    /// </summary>
    public RfqChannel ChannelSource { get; set; }

    /// <summary>
    /// Additional details about the RFQ request.
    /// </summary>
    public object RequestDetails { get; set; } = new();

    /// <summary>
    /// List of file IDs from the upload service.
    /// </summary>
    public List<Guid>? UploadServiceFileIds { get; set; }
}
