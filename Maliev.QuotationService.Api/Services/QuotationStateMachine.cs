using Maliev.QuotationService.Domain.Enums;

namespace Maliev.QuotationService.Api.Services;

/// <summary>
/// State machine for validating quotation status transitions
/// </summary>
public static class QuotationStateMachine
{
    /// <summary>
    /// Validates whether a status transition is allowed
    /// </summary>
    /// <param name="from">Current status</param>
    /// <param name="to">Target status</param>
    /// <returns>True if the transition is valid, false otherwise</returns>
    public static bool IsValidTransition(QuotationStatus from, QuotationStatus to)
    {
        // Define valid status transitions based on business rules
        return (from, to) switch
        {
            // From Draft
            (QuotationStatus.Draft, QuotationStatus.PendingApproval) => true,
            (QuotationStatus.Draft, QuotationStatus.CustomerReview) => true,
            (QuotationStatus.Draft, QuotationStatus.Cancelled) => true,

            // From PendingApproval
            (QuotationStatus.PendingApproval, QuotationStatus.Approved) => true,
            (QuotationStatus.PendingApproval, QuotationStatus.Draft) => true,
            (QuotationStatus.PendingApproval, QuotationStatus.Cancelled) => true,

            // From Approved
            (QuotationStatus.Approved, QuotationStatus.CustomerReview) => true,
            (QuotationStatus.Approved, QuotationStatus.Cancelled) => true,

            // From CustomerReview
            (QuotationStatus.CustomerReview, QuotationStatus.Accepted) => true,
            (QuotationStatus.CustomerReview, QuotationStatus.Expired) => true,
            (QuotationStatus.CustomerReview, QuotationStatus.Cancelled) => true,

            // From Accepted (terminal state, can only expire)
            (QuotationStatus.Accepted, QuotationStatus.Expired) => true,

            // Allow same status (idempotent)
            _ => from == to
        };
    }

    /// <summary>
    /// Gets the list of valid target statuses from a given current status
    /// </summary>
    /// <param name="current">Current status</param>
    /// <returns>List of valid target statuses</returns>
    public static IEnumerable<QuotationStatus> GetValidTransitions(QuotationStatus current)
    {
        return current switch
        {
            QuotationStatus.Draft => new[]
            {
                QuotationStatus.PendingApproval,
                QuotationStatus.CustomerReview,
                QuotationStatus.Cancelled
            },
            QuotationStatus.PendingApproval => new[]
            {
                QuotationStatus.Approved,
                QuotationStatus.Draft,
                QuotationStatus.Cancelled
            },
            QuotationStatus.Approved => new[]
            {
                QuotationStatus.CustomerReview,
                QuotationStatus.Cancelled
            },
            QuotationStatus.CustomerReview => new[]
            {
                QuotationStatus.Accepted,
                QuotationStatus.Expired,
                QuotationStatus.Cancelled
            },
            QuotationStatus.Accepted => new[]
            {
                QuotationStatus.Expired
            },
            _ => Array.Empty<QuotationStatus>()
        };
    }
}
