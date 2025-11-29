using Maliev.QuotationService.Api.Configuration.Settings;
using Maliev.QuotationService.Api.ExternalClients;
using Maliev.QuotationService.Api.ExternalClients.Interfaces;
using Maliev.QuotationService.Api.Services;
using Maliev.QuotationService.Api.Services.Interfaces;
using Maliev.QuotationService.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;

namespace Maliev.QuotationService.Api.Configuration.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddQuotationDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("QuotationDbContext")
            ?? throw new InvalidOperationException("Connection string 'QuotationDbContext' not found.");

        services.AddDbContext<QuotationDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redisSettings = configuration.GetSection("Redis").Get<RedisSettings>() ?? new RedisSettings();

        if (redisSettings.Enabled)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisSettings.ConnectionString;
            });
        }
        else
        {
            // Fallback to in-memory cache for standalone development
            services.AddDistributedMemoryCache();
        }

        return services;
    }

    public static IServiceCollection AddMassTransitWithRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMqSettings = configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>() ?? new RabbitMQSettings();

        services.AddMassTransit(x =>
        {
            if (rabbitMqSettings.Enabled)
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitMqSettings.Host, (ushort)rabbitMqSettings.Port, rabbitMqSettings.VirtualHost, h =>
                    {
                        h.Username(rabbitMqSettings.Username);
                        h.Password(rabbitMqSettings.Password);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                // Fallback to in-memory transport for standalone development
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        return services;
    }

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
