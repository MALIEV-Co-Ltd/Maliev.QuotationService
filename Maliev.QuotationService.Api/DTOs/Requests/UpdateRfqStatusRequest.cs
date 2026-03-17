using Maliev.QuotationService.Domain.Enums;

namespace Maliev.QuotationService.Api.DTOs.Requests;

/// <summary>
/// Request model for updating the status of an RFQ.
/// </summary>
public class UpdateRfqStatusRequest
{
    /// <summary>
    /// The new status to set on the RFQ.
    /// </summary>
    public RfqStatus Status { get; set; }
}
