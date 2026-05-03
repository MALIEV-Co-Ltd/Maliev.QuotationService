namespace Maliev.QuotationService.Api.DTOs.Responses;

/// <summary>
/// Customer response DTO from Customer Service API (subset of fields for billing identity validation)
/// </summary>
public class CustomerApiResponse
{
    /// <summary>
    /// Customer unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Customer first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Customer last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Customer full display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Customer email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Customer mobile phone number
    /// </summary>
    public string? Mobile { get; set; }

    /// <summary>
    /// Customer landline phone number
    /// </summary>
    public string? Landline { get; set; }

    /// <summary>
    /// Company phone number associated with the customer
    /// </summary>
    public string? CompanyPhone { get; set; }

    /// <summary>
    /// Masked Thai National ID (last 2 digits only, for security)
    /// </summary>
    public string? ThaiNationalIdMasked { get; set; }

    /// <summary>
    /// Linked company identifier (if customer belongs to a company)
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Linked company name (if customer belongs to a company)
    /// </summary>
    public string? CompanyName { get; set; }
}
