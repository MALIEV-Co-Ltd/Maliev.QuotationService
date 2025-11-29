using System.Text.Json;

namespace Maliev.QuotationService.Api.ExternalClients.Interfaces;

public interface IMaterialServiceClient
{
    Task<MaterialDto?> GetMaterialByIdAsync(Guid materialId, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetSupportedProcessesAsync(Guid materialId, CancellationToken cancellationToken = default);
}

public record MaterialDto(
    Guid Id,
    string Name,
    string? Category,
    JsonDocument? PhysicalProperties,
    JsonDocument? MechanicalProperties,
    List<string>? SupportedProcesses,
    string AvailabilityStatus);
