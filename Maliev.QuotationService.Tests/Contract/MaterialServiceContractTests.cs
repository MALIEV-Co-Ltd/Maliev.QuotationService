using FluentAssertions;
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
        material.Should().NotBeNull();
        material!.Id.Should().Be(materialId);
        material.Name.Should().NotBeNullOrEmpty();
        material.MechanicalProperties.Should().NotBeNull();

        // Expected properties from Material Service (stored as JsonDocument)
        var mechProps = material.MechanicalProperties!.RootElement;
        mechProps.TryGetProperty("tensileStrength", out _).Should().BeTrue();
        mechProps.TryGetProperty("yieldStrength", out _).Should().BeTrue();
        mechProps.TryGetProperty("hardness", out _).Should().BeTrue();
    }

    [Fact(Skip = "Contract test - requires Material Service to be running")]
    public async Task GetSupportedProcesses_ValidMaterialId_ReturnsProcessList()
    {
        // Arrange
        var materialId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Known test material ID

        // Act
        var processes = await _materialServiceClient.GetSupportedProcessesAsync(materialId);

        // Assert - Document expected contract
        processes.Should().NotBeNull();
        processes.Should().NotBeEmpty();
        processes.Should().AllSatisfy(p => p.Should().NotBeNullOrWhiteSpace());

        // Expected process types from Material Service
        processes.Should().Contain(p =>
            p == "CNC Machining" ||
            p == "3D Printing" ||
            p == "Laser Cutting" ||
            p == "Sheet Metal Forming");
    }
}
