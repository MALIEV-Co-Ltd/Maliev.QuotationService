using Maliev.QuotationService.Api.Configuration.Extensions;
using Maliev.QuotationService.Api.Services.Metrics;
using Maliev.QuotationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;

// Initialize bootstrap logging
using var loggerFactory = LoggerFactory.Create(logBuilder => logBuilder.AddConsole());
var bootstrapLogger = loggerFactory.CreateLogger("Program");

try
{
    Log.StartingHost(bootstrapLogger, "Quotation Service");

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
    builder.Services.AddRateLimiting();

    var app = builder.Build();

    var logger = app.Services.GetRequiredService<ILogger<Program>>();

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

    Log.ServiceStarted(logger, "Quotation Service");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.HostTerminated(bootstrapLogger, ex, "Quotation Service");
    throw;
}
finally
{
    loggerFactory.Dispose();
}

/// <summary>
/// Main program class for the application
/// </summary>
public partial class Program
{
    internal static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Starting {ServiceName} host")]
        public static partial void StartingHost(ILogger logger, string serviceName);

        [LoggerMessage(Level = LogLevel.Critical, Message = "{ServiceName} host terminated unexpectedly during startup")]
        public static partial void HostTerminated(ILogger logger, Exception ex, string serviceName);

        [LoggerMessage(Level = LogLevel.Information, Message = "{ServiceName} started successfully")]
        public static partial void ServiceStarted(ILogger logger, string serviceName);

        [LoggerMessage(Level = LogLevel.Error, Message = "Database migration failed - application may not function correctly")]
        public static partial void MigrationFailed(ILogger logger, Exception exception);
    }
}
