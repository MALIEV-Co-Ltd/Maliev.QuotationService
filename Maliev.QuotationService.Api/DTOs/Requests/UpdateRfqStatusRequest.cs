using Maliev.QuotationService.Domain.Enums;

namespace Maliev.QuotationService.Api.DTOs.Requests;

public class UpdateRfqStatusRequest
{
    public RfqStatus Status { get; set; }
}
