using Maliev.QuotationService.Data.Enums;

namespace Maliev.QuotationService.Api.DTOs.Requests;

public class CreateRfqRequest
{
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhoneNumber { get; set; }
    public RfqChannel ChannelSource { get; set; }
    public object RequestDetails { get; set; } = new();
    public List<Guid>? UploadServiceFileIds { get; set; }
}
