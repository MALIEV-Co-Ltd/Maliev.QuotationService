namespace Maliev.QuotationService.Api.Exceptions;

/// <summary>
/// Indicates that an RFQ cannot enter the converted state without violating its lifecycle or claim state.
/// </summary>
public sealed class RfqConversionConflictException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new conversion conflict.
    /// </summary>
    public RfqConversionConflictException()
        : base("RFQ cannot be converted in its current state.")
    {
    }
}
