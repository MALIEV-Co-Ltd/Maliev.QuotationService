using Maliev.QuotationService.Api.ExternalClients.Interfaces;
using Maliev.QuotationService.Infrastructure.Persistence;
using Maliev.QuotationService.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Text.Json;

namespace Maliev.QuotationService.Tests.Fixtures;

public class IntegrationTestWebAppFactory : BaseIntegrationTestFactory<Program, QuotationDbContext>
{
    protected override void ConfigureEnvironmentVariables()
    {
        base.ConfigureEnvironmentVariables();

        // Set dummy URL for MaterialService to prevent constructor injection failures
        Environment.SetEnvironmentVariable("MaterialService__BaseUrl", "http://localhost:5002");
    }

    protected override void ConfigureAdditionalServices(IServiceCollection services)
    {
        base.ConfigureAdditionalServices(services);

        // Remove existing MaterialServiceClient registration
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMaterialServiceClient));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }

        // Mock MaterialServiceClient
        var mockMaterialService = new Mock<IMaterialServiceClient>();

        // Create test JsonDocuments for material properties
        var mechPropsJson = JsonDocument.Parse(@"{
            ""tensileStrength"": 500,
            ""yieldStrength"": 400,
            ""hardness"": 150
        }");

        var physPropsJson = JsonDocument.Parse(@"{
            ""density"": 2.7,
            ""meltingPoint"": 660
        }");

        mockMaterialService.Setup(x => x.GetMaterialByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken ct) => new MaterialDto(
                id,
                "Test Material",
                "Test Category",
                physPropsJson,
                mechPropsJson,
                new List<string> { "CNC Machining", "3D Printing" },
                "Available"
            ));

        mockMaterialService.Setup(x => x.GetSupportedProcessesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "CNC Machining", "3D Printing" });

        services.AddScoped(_ => mockMaterialService.Object);
    }
}
