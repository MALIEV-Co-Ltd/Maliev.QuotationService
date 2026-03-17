using Maliev.QuotationService.Domain.Enums;

namespace Maliev.QuotationService.Api.DTOs.Responses;

/// <summary>
/// Response model for an RFQ.
/// </summary>
public class RfqResponse
{
    /// <summary>
    /// The unique identifier of the RFQ.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The customer details.
    /// </summary>
    public CustomerDto Customer { get; set; } = new();

    /// <summary>
    /// The source channel where the RFQ originated.
    /// </summary>
    public RfqChannel ChannelSource { get; set; }

    /// <summary>
    /// The current status of the RFQ.
    /// </summary>
    public RfqStatus Status { get; set; }

    /// <summary>
    /// Additional details about the RFQ request.
    /// </summary>
    public object? RequestDetails { get; set; }

    /// <summary>
    /// The staff user assigned to this RFQ.
    /// </summary>
    public string? AssignedStaffUserId { get; set; }

    /// <summary>
    /// The timestamp when the RFQ was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The timestamp when the RFQ was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Response model for customer information.
/// </summary>
public class CustomerDto
{
    /// <summary>
    /// The unique identifier of the customer.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The customer's email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// The customer's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The customer's phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }
}
