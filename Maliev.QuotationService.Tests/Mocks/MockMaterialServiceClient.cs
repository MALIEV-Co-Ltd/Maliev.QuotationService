using System.Text.Json;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;

namespace Maliev.QuotationService.Tests.Mocks;

public class MockMaterialServiceClient : IMaterialServiceClient
{
    private static readonly Guid KnownMaterialId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Task<MaterialDto?> GetMaterialByIdAsync(Guid materialId, CancellationToken cancellationToken = default)
    {
        if (materialId != KnownMaterialId)
        {
            return Task.FromResult<MaterialDto?>(null);
        }

        var mechanicalProperties = JsonDocument.Parse(@"{
            ""tensileStrength"": 500,
            ""yieldStrength"": 300,
            ""hardness"": 150
        }");

        var material = new MaterialDto(
            Id: materialId,
            Name: "Test Material",
            Category: "Metal",
            PhysicalProperties: null,
            MechanicalProperties: mechanicalProperties,
            SupportedProcesses: new List<string> { "CNC Machining", "3D Printing", "Laser Cutting", "Sheet Metal Forming" },
            AvailabilityStatus: "InStock"
        );

        return Task.FromResult<MaterialDto?>(material);
    }

    public Task<IEnumerable<string>> GetSupportedProcessesAsync(Guid materialId, CancellationToken cancellationToken = default)
    {
        if (materialId != KnownMaterialId)
        {
            return Task.FromResult<IEnumerable<string>>(Enumerable.Empty<string>());
        }

        return Task.FromResult<IEnumerable<string>>(new[]
        {
            "CNC Machining",
            "3D Printing",
            "Laser Cutting",
            "Sheet Metal Forming"
        });
    }
}
