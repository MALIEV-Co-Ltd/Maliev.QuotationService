using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Maliev.QuotationService.Api.Configuration.Extensions;
using Maliev.QuotationService.Api.Services.Metrics;
using Maliev.QuotationService.Data;
using Maliev.Aspire.ServiceDefaults;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Microsoft.Extensions.Logging;
using Scalar.AspNetCore;

// Initialize bootstrap logging
using var loggerFactory = LoggerFactory.Create(logBuilder => logBuilder.AddConsole());
var bootstrapLogger = loggerFactory.CreateLogger("Program");

try
{
    bootstrapLogger.LogInformation("Starting Quotation Service host");

    var builder = WebApplication.CreateBuilder(args);

    // --- Secrets & Configuration ---
    builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

    // --- Infrastructure & Observability ---
    builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
    builder.AddStandardMiddleware(options =>
    {
        options.EnableRequestLogging = true;
    });
    builder.AddServiceMeters("quotations-meter"); // Register service meters for OpenTelemetry business metrics

    // Add database context
    builder.AddPostgresDbContext<QuotationDbContext>("QuotationDbContext");

    // Add caching (Redis or in-memory fallback)
    builder.AddRedisDistributedCache("quotation:");

    // Add message bus (RabbitMQ or in-memory fallback)
    builder.AddMassTransitWithRabbitMq(x =>
    {
        x.AddConsumer<Maliev.QuotationService.Api.Consumers.FileDeletedEventConsumer>();
        x.AddConsumer<Maliev.QuotationService.Api.Consumers.FileAnalyzedEventConsumer>();
    });

    // --- API Configuration ---
    builder.AddDefaultCors(); // CORS from CORS:AllowedOrigins config
    builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

    // Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
    if (!builder.Environment.IsProduction())
    {
        builder.AddStandardOpenApi(
            title: "MALIEV Quotation Service API",
            description: "Quotation and RFQ management service. Handles quotation creation, editing, and tracking.");
    }

    // Add external service clients with resilience
    builder.AddIAMServiceClient("quotation");
    builder.Services.AddExternalServiceClients(builder.Configuration);

    // Add authentication
    builder.AddJwtAuthentication();
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("Customer", policy => policy.RequireRole("Customer"));
        options.AddPolicy("Employee", policy => policy.RequireRole("Employee"));
        options.AddPolicy("Manager", policy => policy.RequireRole("Manager"));
        options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
        options.AddPolicy("EmployeeOrHigher", policy =>
            policy.RequireRole("Employee", "Manager", "Admin"));
    });

    // Add application services
    builder.Services.AddApplicationServices();
    builder.Services.AddSingleton<MetricsService>();

    // Add rate limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1)
                }));

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = 429;
            await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken: token);
        };
    });

    var app = builder.Build();
    var logger = app.Services.GetRequiredService<ILogger<Maliev.QuotationService.Api.Program>>();

    await app.MigrateDatabaseAsync<QuotationDbContext>();

    // Configure middleware pipeline
    app.UseStandardMiddleware();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseRouting();
    app.UseCors();

    app.UseAuthentication();
    app.UseMiddleware<Maliev.QuotationService.Api.Middleware.AuthorizationAuditMiddleware>();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapControllers();

    app.MapDefaultEndpoints(servicePrefix: "quotation");

    // Map OpenAPI and Scalar documentation (dev/staging only)
    app.MapApiDocumentation(servicePrefix: "quotation");

    logger.LogInformation("QuotationService started successfully");
    await app.RunAsync();
}
catch (Exception ex)
{
    bootstrapLogger.LogCritical(ex, "Quotation Service host terminated unexpectedly during startup");
    throw;
}
finally
{
    loggerFactory.Dispose();
}

/// <summary>
/// Main program class for the application
/// </summary>
namespace Maliev.QuotationService.Api
{
    public partial class Program
    {
        internal static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Information, Message = "QuotationService started successfully")]
            public static partial void ServiceStarted(ILogger logger);

            [LoggerMessage(Level = LogLevel.Error, Message = "Database migration failed - application may not function correctly")]
            public static partial void MigrationFailed(ILogger logger, Exception exception);
        }
    }
}
