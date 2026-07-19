namespace Maliev.QuotationService.Api.DTOs.Requests;

/// <summary>
/// Request model for updating an existing RFQ.
/// </summary>
public class UpdateRfqRequest
{
    /// <summary>
    /// Updated request details for the RFQ.
    /// </summary>
    public object? RequestDetails { get; set; }

    /// <summary>
    /// The staff user to assign the RFQ to.
    /// </summary>
    public string? AssignedStaffUserId { get; set; }
}
