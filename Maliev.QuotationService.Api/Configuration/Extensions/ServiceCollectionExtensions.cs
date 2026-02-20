using Maliev.QuotationService.Api.ExternalClients;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;
using Maliev.QuotationService.Api.Services.IAM;
using Maliev.QuotationService.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Maliev.QuotationService.Api.Configuration.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IQuotationService, Maliev.QuotationService.Api.Services.QuotationService>();
        services.AddScoped<IRfqService, Maliev.QuotationService.Api.Services.RfqService>();
        services.AddScoped<ICustomerMatchingService, Maliev.QuotationService.Api.Services.CustomerMatchingService>();
        services.AddScoped<IAnalyticsService, Maliev.QuotationService.Api.Services.AnalyticsService>();

        // IAM & Authorization
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddIAMRegistration<QuotationIAMRegistrationService>("quotation");

        return services;
    }

    public static IServiceCollection AddExternalServiceClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<Maliev.QuotationService.Api.Middleware.HeaderForwardingHandler>();

        services.AddServiceClient<IMaterialServiceClient, MaterialServiceClient>(configuration, "MaterialService")
            .AddHttpMessageHandler<Maliev.QuotationService.Api.Middleware.HeaderForwardingHandler>();

        services.AddServiceClient<ICurrencyServiceClient, CurrencyServiceClient>(configuration, "CurrencyService")
            .AddHttpMessageHandler<Maliev.QuotationService.Api.Middleware.HeaderForwardingHandler>();

        services.AddServiceClient<IUploadServiceClient, UploadServiceClient>(configuration, "UploadService")
            .AddHttpMessageHandler<Maliev.QuotationService.Api.Middleware.HeaderForwardingHandler>();

        services.AddServiceClient<IPdfServiceClient, PdfServiceClient>(configuration, "PdfService")
            .AddHttpMessageHandler<Maliev.QuotationService.Api.Middleware.HeaderForwardingHandler>();

        services.AddServiceClient<ICustomerServiceClient, CustomerServiceClient>(configuration, "CustomerService")
            .AddHttpMessageHandler<Maliev.QuotationService.Api.Middleware.HeaderForwardingHandler>();

        return services;
    }
}
