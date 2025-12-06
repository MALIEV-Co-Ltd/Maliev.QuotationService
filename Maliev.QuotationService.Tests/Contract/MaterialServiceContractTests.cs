using Xunit;
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
        _materialServiceClient = Scope.ServiceProvider.GetRequiredService<IMaterialServiceClient>();
    }

    [Fact(Skip = "Contract test - requires Material Service to be running")]
    public async Task GetMaterialById_ValidId_ReturnsData()
    {
        // Arrange
        var materialId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Known test material ID

        // Act
        var material = await _materialServiceClient.GetMaterialByIdAsync(materialId);

        // Assert - Document expected contract
        Assert.NotNull(material);
        Assert.Equal(materialId, material.Id);
        Assert.False(string.IsNullOrEmpty(material.Name));
        Assert.NotNull(material.MechanicalProperties);

        // Expected properties from Material Service (stored as JsonDocument)
        var mechProps = material.MechanicalProperties!.RootElement;
        Assert.True(mechProps.TryGetProperty("tensileStrength", out _));
        Assert.True(mechProps.TryGetProperty("yieldStrength", out _));
        Assert.True(mechProps.TryGetProperty("hardness", out _));
    }

    [Fact(Skip = "Contract test - requires Material Service to be running")]
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
