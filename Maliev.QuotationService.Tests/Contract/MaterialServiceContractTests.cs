using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;
using Maliev.QuotationService.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.QuotationService.Tests.Contract;

/// <summary>
/// Contract tests verify integration expectations with external services.
/// These tests document the expected behavior and data format from Material Service.
/// </summary>
public class MaterialServiceContractTests : BaseIntegrationTest
{
    private readonly IMaterialServiceClient _materialServiceClient;

    public MaterialServiceContractTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        // Set up the mock handler for the Material Service client
        factory.MockMaterialServiceHandler = (request, cancellationToken) =>
        {
            var materialId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // This is the ID used in the tests

            if (request.RequestUri!.AbsolutePath.Contains($"/materials/v1/Materials/{materialId}/processes"))
            {
                var response = new
                {
                    processes = new[] { "CNC Machining", "3D Printing", "Laser Cutting", "Sheet Metal Forming" }
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(response)
                });
            }

            if (request.RequestUri!.AbsolutePath.Contains($"/materials/v1/Materials/{materialId}"))
            {
                var response = new
                {
                    id = materialId,
                    name = "Aluminum 6061",
                    code = "AL6061",
                    description = "Common aluminum alloy",
                    pricePerUnit = 10.5m,
                    stockLevel = 100,
                    supplierId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
                    supplierName = "AluSuppliers Inc.",
                    manufacturingProcesses = new[]
                    {
                        new { id = Guid.NewGuid(), name = "CNC Machining" },
                        new { id = Guid.NewGuid(), name = "Laser Cutting" }
                    },
                    availableColors = new[]
                    {
                        new { id = Guid.NewGuid(), name = "Silver", hexCode = "#C0C0C0" }
                    },
                    postProcessingMethods = new[]
                    {
                        new { id = Guid.NewGuid(), name = "Anodizing" }
                    },
                    mechanicalProperties = new[]
                    {
                        new { mechanicalPropertyId = Guid.NewGuid(), mechanicalPropertyName = "tensileStrength", unit = "MPa", value = 310 },
                        new { mechanicalPropertyId = Guid.NewGuid(), mechanicalPropertyName = "yieldStrength", unit = "MPa", value = 276 },
                        new { mechanicalPropertyId = Guid.NewGuid(), mechanicalPropertyName = "hardness", unit = "HB", value = 95 }
                    },
                    createdBy = "system",
                    createdAt = DateTime.UtcNow.AddDays(-30),
                    updatedBy = (string?)null,
                    updatedAt = (DateTime?)null,
                    version = 1,
                    active = true
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(response)
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        };
        
        _materialServiceClient = Scope.ServiceProvider.GetRequiredService<IMaterialServiceClient>();
    }

    [Fact]
    public async Task GetMaterialById_ValidId_ReturnsData()
    {
        // Arrange
        var materialId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Known test material ID

        // Act
        var material = await _materialServiceClient.GetMaterialByIdAsync(materialId);

        // Assert - Document expected contract
        Assert.NotNull(material);
        Assert.Equal(materialId, material!.Id);
        Assert.False(string.IsNullOrEmpty(material.Name));
        Assert.NotNull(material.MechanicalProperties);

        // Expected properties from Material Service (stored as JsonDocument)
        Assert.NotNull(material.MechanicalProperties);
        Assert.Equal(JsonValueKind.Array, material.MechanicalProperties.RootElement.ValueKind);

        var mechanicalProperties = material.MechanicalProperties.RootElement.EnumerateArray().ToList();
        Assert.Contains(mechanicalProperties, p => p.GetProperty("mechanicalPropertyName").GetString() == "tensileStrength");
        Assert.Contains(mechanicalProperties, p => p.GetProperty("mechanicalPropertyName").GetString() == "yieldStrength");
        Assert.Contains(mechanicalProperties, p => p.GetProperty("mechanicalPropertyName").GetString() == "hardness");
    }

    [Fact]
    public async Task GetSupportedProcesses_ValidMaterialId_ReturnsProcessList()
    {
        // Arrange
        var materialId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Known test material ID

        // Act
        var processes = await _materialServiceClient.GetSupportedProcessesAsync(materialId);

        // Assert - Document expected contract
        Assert.NotNull(processes);
        Assert.NotEmpty(processes);
        Assert.All(processes, p => Assert.False(string.IsNullOrWhiteSpace(p)));

        // Expected process types from Material Service
        Assert.Contains(processes, p =>
            p == "CNC Machining" ||
            p == "3D Printing" ||
            p == "Laser Cutting" ||
            p == "Sheet Metal Forming");
    }
}
