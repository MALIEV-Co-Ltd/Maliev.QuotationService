namespace Maliev.QuotationService.Api.Exceptions;

/// <summary>
/// Indicates that an RFQ is absent, not owned by the quotation customer, or not in the required lifecycle state.
/// </summary>
public sealed class RfqNotFoundException : KeyNotFoundException
{
    /// <summary>
    /// Initializes a new instance for the requested RFQ.
    /// </summary>
    /// <param name="rfqId">The RFQ identifier.</param>
    public RfqNotFoundException(Guid rfqId)
        : base($"RFQ with ID {rfqId} not found")
    {
    }
}
