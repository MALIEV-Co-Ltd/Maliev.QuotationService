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
            var materialId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            if (request.RequestUri!.AbsolutePath.Contains($"/api/v1/materials/{materialId}/processes"))
            {
                var response = new
                {
                    Processes = new[] { "CNC Machining", "3D Printing", "Laser Cutting", "Sheet Metal Forming" }
                };
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(response)
                });
            }

            if (request.RequestUri!.AbsolutePath.Contains($"/api/v1/materials/{materialId}"))
            {
                var response = new
                {
                    id = materialId,
                    name = "Aluminum 6061",
                    mechanicalProperties = JsonDocument.Parse("{\"tensileStrength\": 310, \"yieldStrength\": 276, \"hardness\": 95}")
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
        var mechProps = material.MechanicalProperties!.RootElement;
        Assert.True(mechProps.TryGetProperty("tensileStrength", out _));
        Assert.True(mechProps.TryGetProperty("yieldStrength", out _));
        Assert.True(mechProps.TryGetProperty("hardness", out _));
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
