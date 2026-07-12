using Maliev.QuotationService.Api.ExternalClients;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;
using Maliev.QuotationService.Api.Services.IAM;
using Maliev.QuotationService.Application.Authorization;
using Maliev.QuotationService.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Maliev.QuotationService.Api.Configuration.Extensions;

/// <summary>
/// Extension methods for registering application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds application services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IQuotationService, Maliev.QuotationService.Api.Services.QuotationService>();
        services.AddScoped<IRfqService, Maliev.QuotationService.Api.Services.RfqService>();
        services.AddScoped<ICustomerMatchingService, Maliev.QuotationService.Api.Services.CustomerMatchingService>();
        services.AddScoped<IAnalyticsService, Maliev.QuotationService.Api.Services.AnalyticsService>();

        // IAM Registration - uses centralized PermissionAuthorizationPolicyProvider from ServiceDefaults
        services.AddIAMRegistration<QuotationIAMRegistrationService>("quotation");

        return services;
    }

    /// <summary>
    /// Adds external service clients to the service collection.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddExternalServiceClients(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddTransient<Maliev.QuotationService.Api.Middleware.HeaderForwardingHandler>();

        services.AddServiceClient<IMaterialServiceClient, MaterialServiceClient>(configuration, "MaterialService")
            .AddHttpMessageHandler<Maliev.QuotationService.Api.Middleware.HeaderForwardingHandler>();

        services.AddServiceClient<ICurrencyServiceClient, CurrencyServiceClient>(configuration, "CurrencyService")
            .AddHttpMessageHandler<Maliev.QuotationService.Api.Middleware.HeaderForwardingHandler>();

        services.AddServiceClient<IUploadServiceClient, UploadServiceClient>(configuration, "UploadService")
            .AddHttpMessageHandler<Maliev.QuotationService.Api.Middleware.HeaderForwardingHandler>();

        services.AddServiceClient<IPdfServiceClient, PdfServiceClient>(configuration, "PdfService")
            .AddHttpMessageHandler<Maliev.QuotationService.Api.Middleware.HeaderForwardingHandler>();

        builder.AddAuthenticatedServiceClient<ICustomerServiceClient, CustomerServiceClient>(
            "CustomerService",
            sourceServiceName: "quotation");

        builder.AddAuthenticatedServiceClient<IProjectServiceClient, ProjectServiceClient>(
            "ProjectService",
            sourceServiceName: "quotation");

        return services;
    }
}
