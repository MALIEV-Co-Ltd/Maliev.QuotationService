using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Maliev.QuotationService.Api.Configuration.Extensions;
using Maliev.QuotationService.Api.Middleware;
using Maliev.QuotationService.Api.Services.Metrics;
using Maliev.QuotationService.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- Secrets & Configuration ---
builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

// --- Infrastructure & Observability ---
builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
builder.AddServiceMeters("quotations-meter"); // Register service meters for OpenTelemetry business metrics

// Add database context
builder.AddPostgresDbContext<QuotationDbContext>("QuotationDbContext", configureOptions: options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("QuotationDbContext"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(QuotationDbContext).Assembly.GetName().Name));
});

// Add caching (Redis or in-memory fallback)
builder.AddRedisDistributedCache("quotation:");

// Add message bus (RabbitMQ or in-memory fallback)
builder.AddMassTransitWithRabbitMq();

// --- API Configuration ---
builder.AddDefaultCors(); // CORS from CORS:AllowedOrigins config
builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

// Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
if (!builder.Environment.IsProduction())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi("v1", options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "MALIEV Quotation Service API";
            document.Info.Version = "v1";
            document.Info.Description = "Quotation and RFQ management service.";
            return Task.CompletedTask;
        });
    });
}

// Add external service clients with resilience
builder.Services.AddExternalServiceClients(builder.Configuration);

// Add application services
builder.Services.AddApplicationServices();
builder.Services.AddSingleton<MetricsService>();

// Add controllers
builder.Services.AddControllers();

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
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Run database migrations on startup (skip in Testing environment)
if (!app.Environment.IsEnvironment("Testing"))
{
    try
    {
        await app.MigrateDatabaseAsync<QuotationDbContext>();
    }
    catch (Exception ex)
    {
        Log.MigrationFailed(logger, ex);
        // Don't throw - allow app to start for debugging
    }
}

// Configure middleware pipeline
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Redirect root to /quotation (custom legacy behavior kept)
app.MapGet("/", () => Results.Redirect("/quotation"));

app.MapControllers();

app.MapDefaultEndpoints(servicePrefix: "quotation");

// Map OpenAPI and Scalar documentation (dev/staging only)
app.MapApiDocumentation(servicePrefix: "quotation");

Log.ServiceStarted(logger);

await app.RunAsync();

/// <summary>
/// Main program class for the application
/// </summary>
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
