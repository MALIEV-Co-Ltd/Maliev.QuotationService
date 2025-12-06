using Maliev.QuotationService.Api.Configuration.Settings;
using Maliev.QuotationService.Api.ExternalClients;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;
using Maliev.QuotationService.Api.Services;
using Maliev.QuotationService.Api.Services.Interfaces;
using Microsoft.Extensions.Http.Resilience;

namespace Maliev.QuotationService.Api.Configuration.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExternalServiceClients(this IServiceCollection services, IConfiguration configuration)
    {
        var externalServicesSettings = configuration.GetSection("ExternalServices").Get<ExternalServicesSettings>()
            ?? throw new InvalidOperationException("External services settings not configured.");

        // Currency Service Client
        services.AddHttpClient<ICurrencyServiceClient, CurrencyServiceClient>(client =>
        {
            client.BaseAddress = new Uri(externalServicesSettings.Currency.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(externalServicesSettings.Currency.TimeoutInSeconds);
        })
        .AddStandardResilienceHandler();

        // Material Service Client
        services.AddHttpClient<IMaterialServiceClient, MaterialServiceClient>(client =>
        {
            client.BaseAddress = new Uri(externalServicesSettings.Material.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(externalServicesSettings.Material.TimeoutInSeconds);
        })
        .AddStandardResilienceHandler();

        // Upload Service Client
        services.AddHttpClient<IUploadServiceClient, UploadServiceClient>(client =>
        {
            client.BaseAddress = new Uri(externalServicesSettings.Upload.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(externalServicesSettings.Upload.TimeoutInSeconds);
        })
        .AddStandardResilienceHandler();

        // PDF Service Client
        services.AddHttpClient<IPdfServiceClient, PdfServiceClient>(client =>
        {
            client.BaseAddress = new Uri(externalServicesSettings.PDF.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(externalServicesSettings.PDF.TimeoutInSeconds);
        })
        .AddStandardResilienceHandler();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register business services
        services.AddScoped<IRfqService, RfqService>();
        services.AddScoped<IQuotationService, Api.Services.QuotationService>();

        return services;
    }
}
