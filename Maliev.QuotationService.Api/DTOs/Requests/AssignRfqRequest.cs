namespace Maliev.QuotationService.Api.DTOs.Requests;

/// <summary>
/// Request model for assigning an RFQ to a staff member.
/// </summary>
public class AssignRfqRequest
{
    /// <summary>
    /// The unique identifier of the staff user to assign the RFQ to.
    /// </summary>
    public string AssignedStaffUserId { get; set; } = string.Empty;
}
