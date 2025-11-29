using Prometheus;

namespace Maliev.QuotationService.Api.Configuration.Extensions;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Standard .NET logging is configured in Program.cs for Aspire compatibility
        // Aspire automatically provides structured logging support

        // Configure Prometheus metrics
        services.UseHttpClientMetrics();

        return services;
    }

    public static WebApplication UseObservability(this WebApplication app)
    {
        // Add Prometheus HTTP metrics middleware
        app.UseHttpMetrics();

        // Map Prometheus metrics endpoint
        app.MapMetrics("/quotation/metrics");

        return app;
    }
}
