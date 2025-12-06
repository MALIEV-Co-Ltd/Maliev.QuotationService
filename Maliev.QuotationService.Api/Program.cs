
using Maliev.QuotationService.Api.Configuration.Extensions;
using Maliev.QuotationService.Api.Middleware;
using Maliev.QuotationService.Data;
using Prometheus;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddControllers();

// Add API versioning
builder.AddDefaultApiVersioning();



// Add database context
builder.AddPostgresDbContext<QuotationDbContext>("QuotationDbContext");

// Add caching (Redis or in-memory fallback)
builder.AddRedisDistributedCache("Quotation");

// Add message bus (RabbitMQ or in-memory fallback)
builder.AddMassTransitWithRabbitMq();

// Add external service clients with resilience
builder.Services.AddExternalServiceClients(builder.Configuration);

// Add application services
builder.Services.AddApplicationServices();

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

// Add CORS
builder.AddDefaultCors();


var app = builder.Build();

// Configure middleware pipeline
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Enable Prometheus metrics
app.UseHttpMetrics();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapApiDocumentation("quotation");
}

app.UseHttpsRedirection();

// Redirect root to /quotation
app.MapGet("/", () => Results.Redirect("/quotation"));

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.MapDefaultEndpoints("quotation");


var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Starting Quotation Service");
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application terminated unexpectedly");
    throw;
}

// Make Program class accessible to tests
public partial class Program { }
