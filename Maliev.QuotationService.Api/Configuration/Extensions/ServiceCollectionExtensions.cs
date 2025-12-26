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

        // IAM & Authorization
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddHostedService<QuotationIAMRegistrationService>();

        return services;
    }

    public static IServiceCollection AddExternalServiceClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IMaterialServiceClient, MaterialServiceClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["ExternalServices:MaterialService:BaseUrl"]!);
        }).AddStandardResilienceHandler();

        // Register IAM HttpClient for use via IHttpClientFactory
        services.AddHttpClient("IAM", client =>
        {
            var baseUrl = configuration["ExternalServices:IAM:BaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl);
            }
        }).AddStandardResilienceHandler();

        return services;
    }
}
