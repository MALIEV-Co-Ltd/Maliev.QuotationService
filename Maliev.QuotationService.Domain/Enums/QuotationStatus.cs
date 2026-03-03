namespace Maliev.QuotationService.Domain.Enums;

public enum QuotationStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    CustomerReview = 4,
    Accepted = 5,
    Expired = 6,
    Cancelled = 7
}
