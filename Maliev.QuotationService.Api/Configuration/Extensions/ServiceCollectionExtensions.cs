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
        services.AddServiceClient<IMaterialServiceClient, MaterialServiceClient>(configuration, "MaterialService");
        services.AddServiceClient<ICurrencyServiceClient, CurrencyServiceClient>(configuration, "CurrencyService");
        services.AddServiceClient<IUploadServiceClient, UploadServiceClient>(configuration, "UploadService");
        services.AddServiceClient<IPdfServiceClient, PdfServiceClient>(configuration, "PdfService");

        return services;
    }
}
