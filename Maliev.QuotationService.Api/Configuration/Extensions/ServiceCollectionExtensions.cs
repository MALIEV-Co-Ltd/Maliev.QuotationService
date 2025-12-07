using Maliev.QuotationService.Api.ExternalClients;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;
using Maliev.QuotationService.Api.Services.Interfaces;

namespace Maliev.QuotationService.Api.Configuration.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IQuotationService, Maliev.QuotationService.Api.Services.QuotationService>();
        services.AddScoped<IRfqService, Maliev.QuotationService.Api.Services.RfqService>();
        return services;
    }

    public static IServiceCollection AddExternalServiceClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IMaterialServiceClient, MaterialServiceClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["ExternalServices:MaterialService:BaseUrl"]!);
        }).AddStandardResilienceHandler();

        return services;
    }
}
