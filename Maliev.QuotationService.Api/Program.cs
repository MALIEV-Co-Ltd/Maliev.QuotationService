using Maliev.QuotationService.Api.Configuration.Extensions;
using Maliev.QuotationService.Api.Middleware;
using Maliev.QuotationService.Data;

var builder = WebApplication.CreateBuilder(args);

// --- Secrets & Configuration ---
builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

// --- Infrastructure & Observability ---
builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
builder.AddServiceMeters("quotation"); // Register service meters for OpenTelemetry business metrics

builder.AddPostgresDbContext<QuotationDbContext>(connectionStringName: "QuotationDbContext"); // PostgreSQL with retry logic
builder.AddRedisDistributedCache(instanceName: "Quotation:"); // Redis with in-memory fallback
builder.AddMassTransitWithRabbitMq(); // RabbitMQ message bus (non-blocking startup)

// --- API Configuration ---
builder.AddDefaultCors(); // CORS from CORS:AllowedOrigins config
builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

// JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
builder.AddJwtAuthentication();

// Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
if (!builder.Environment.IsProduction())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi("v1", options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "Maliev Quotation Service API";
            document.Info.Version = "v1";
            document.Info.Description = "Quotation and RFQ management service. Handles quotation creation and versioning, status workflows (draft/pending/approved/rejected), manager approval process, internal notes, version history tracking, and PDF generation for customer delivery.";
            return Task.CompletedTask;
        });
    });
}

builder.Services.AddControllers();

// External service clients with resilience
builder.Services.AddExternalServiceClients(builder.Configuration);

// Application services
builder.Services.AddApplicationServices();

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Customer", policy => policy.RequireRole("Customer"));
    options.AddPolicy("Employee", policy => policy.RequireRole("Employee"));
    options.AddPolicy("Manager", policy => policy.RequireRole("Manager"));
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("EmployeeOrHigher", policy =>
        policy.RequireRole("Employee", "Manager", "Admin"));
});

// Rate limiting
builder.Services.AddRateLimiting();

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
        logger.LogError(ex, "Database migration failed - application may not function correctly");
        // Don't throw - allow app to start for debugging
    }
}

// Configure middleware pipeline
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

// Redirect root to /quotation
app.MapGet("/", () => Results.Redirect("/quotation"));

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

// Map Aspire default endpoints (/health, /alive, /metrics)
app.MapDefaultEndpoints(servicePrefix: "quotation");

// Map OpenAPI and Scalar documentation (dev/staging only)
app.MapApiDocumentation(servicePrefix: "quotation");

try
{
    logger.LogInformation("Starting Quotation Service");
    await app.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application terminated unexpectedly");
    throw;
}

/// <summary>
/// Main program class for the Quotation Service API.
/// </summary>
public partial class Program { }
