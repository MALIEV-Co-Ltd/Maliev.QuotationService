using Maliev.QuotationService.Data.Enums;

namespace Maliev.QuotationService.Api.DTOs.Requests;

public class UpdateRfqStatusRequest
{
    public RfqStatus Status { get; set; }
}
