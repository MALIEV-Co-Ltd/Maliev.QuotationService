using System.Net;
using System.Net.Http.Json;
using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Api.Services.IAM;
using Maliev.QuotationService.Tests.Testing;
using Xunit;

namespace Maliev.QuotationService.Tests.Integration;

public class AnalyticsControllerTests : IClassFixture<BaseIntegrationTestFactory<Program, Maliev.QuotationService.Infrastructure.Persistence.QuotationDbContext>>
{
    private readonly BaseIntegrationTestFactory<Program, Maliev.QuotationService.Infrastructure.Persistence.QuotationDbContext> _factory;
    private readonly HttpClient _adminClient;

    public AnalyticsControllerTests(BaseIntegrationTestFactory<Program, Maliev.QuotationService.Infrastructure.Persistence.QuotationDbContext> factory)
    {
        _factory = factory;
        // Use authenticated client with admin role
        _adminClient = factory.CreateAuthenticatedClient(roles: new[] { QuotationPredefinedRoles.Admin });
    }

    [Fact]
    public async Task GetConversionRates_ShouldReturnSuccess()
    {
        // Act
        var response = await _adminClient.GetAsync("/quotation/v1/analytics/conversion-rates");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTurnaroundTime_ShouldReturnSuccess()
    {
        // Act
        var response = await _adminClient.GetAsync("/quotation/v1/analytics/turnaround-time");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAbandonedRfqs_ShouldReturnSuccess()
    {
        // Act
        var response = await _adminClient.GetAsync("/quotation/v1/analytics/abandoned-rfqs");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
