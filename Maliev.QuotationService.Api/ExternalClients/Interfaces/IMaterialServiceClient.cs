using System.Text.Json;

namespace Maliev.QuotationService.Api.ExternalClients.Interfaces;

/// <summary>
/// Client interface for interacting with the Material Service API.
/// </summary>
public interface IMaterialServiceClient
{
    /// <summary>
    /// Gets a material by its unique identifier.
    /// </summary>
    /// <param name="materialId">The unique identifier of the material.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The material details if found, otherwise null.</returns>
    Task<MaterialDto?> GetMaterialByIdAsync(Guid materialId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the supported manufacturing processes for a material.
    /// </summary>
    /// <param name="materialId">The unique identifier of the material.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of supported process names.</returns>
    Task<IEnumerable<string>> GetSupportedProcessesAsync(Guid materialId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents material data from the Material Service.
/// </summary>
public record MaterialDto(
    Guid Id,
    string Name,
    string? Category,
    JsonDocument? PhysicalProperties,
    JsonDocument? MechanicalProperties,
    List<string>? SupportedProcesses,
    string AvailabilityStatus);
