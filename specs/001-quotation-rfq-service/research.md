# Research: Unified Quotation and RFQ Management Service

**Feature**: 001-quotation-rfq-service
**Date**: 2025-11-28
**Phase**: Phase 0 - Technology Research & Best Practices

## Overview

This document consolidates research findings, technology decisions, and best practices for implementing the Unified Quotation and RFQ Management Service. All technical choices are predetermined by the MALIEV microservices technical stack and architectural plan.

## Technology Stack Decisions

### 1. Framework & Runtime

**Decision**: .NET 10 WebAPI with ASP.NET Core 10.0

**Rationale**:
- Latest LTS release with long-term support through 2027
- Native support for high-performance HTTP pipelines
- Built-in dependency injection, configuration, and middleware infrastructure
- Excellent tooling for OpenAPI/Swagger documentation
- Strong ecosystem for microservices patterns

**Alternatives Considered**:
- .NET 9: Rejected - not an LTS release, shorter support window
- .NET 8: Rejected - although LTS, .NET 10 provides newer features and will have longer active support

### 2. Data Access & Persistence

**Decision**: Entity Framework Core 10.0.0 with Npgsql 10.0.0 provider for PostgreSQL 18

**Rationale**:
- EF Core provides robust ORM with migration support, query optimization, and change tracking
- Npgsql is the official PostgreSQL provider with excellent performance characteristics
- PostgreSQL 18 offers advanced JSON support (JSONB), full-text search, and superior indexing capabilities
- Native support for optimistic concurrency via `RowVersion` (timestamp in PostgreSQL)
- Excellent support for complex queries needed for analytics endpoints

**Best Practices**:
- Use Fluent API configurations (not Data Annotations) for entity mapping in separate configuration classes
- Implement indexes on frequently queried fields (customer identifiers, dates, channel sources, statuses)
- Use `AsNoTracking()` for read-only queries to improve performance
- Implement proper connection pooling with `Npgsql.EnableRetry Policy`
- Use PostgreSQL-specific features: JSONB for flexible metadata, GIN indexes for full-text search on notes

**Alternatives Considered**:
- Dapper: Rejected - loses type safety and requires manual SQL for complex queries
- In-memory database (testing): Explicitly FORBIDDEN by constitution (Principle IV)

### 3. HTTP Resilience & External Service Integration

**Decision**: Microsoft.Extensions.Http.Resilience 10.0.0 with `AddStandardResilienceHandler()`

**Rationale**:
- Provides built-in retry, circuit breaker, timeout, and rate limiting via Polly v8
- `.AddStandardResilienceHandler()` applies sensible defaults without manual Polly configuration
- Integrates seamlessly with typed HttpClient pattern
- Distributed tracing propagation built-in

**Configuration Pattern**:
```csharp
services.AddHttpClient<ICurrencyServiceClient, CurrencyServiceClient>(client =>
{
    client.BaseAddress = new Uri(settings.ExternalServices.Currency.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.ExternalServices.Currency.TimeoutInSeconds);
})
.AddStandardResilienceHandler(); // Retry (3x exponential backoff) + Circuit Breaker + Timeout
```

**Best Practices**:
- Circuit breaker opens after 50% failure rate over 30-second window (Polly v8 defaults)
- Exponential backoff: base delay 2s, max delay 30s
- Circuit breaker remains open for 30 seconds before attempting recovery (half-open state)
- Emit metrics on circuit breaker state changes for observability (FR-044)

**Alternatives Considered**:
- Direct Polly package reference: Rejected - Microsoft.Extensions.Http.Resilience provides higher-level abstraction
- Manual retry logic: Rejected - duplicates effort and loses standardization

### 4. Authentication & Authorization

**Decision**: JWT Bearer authentication with RSA public key validation (asymmetric)

**Rationale**:
- RSA asymmetric keys allow validation without sharing private keys across services
- Public key can be distributed to all microservices for token verification
- Supports role-based authorization policies required by FR-029, FR-030

**Implementation Pattern**:
```csharp
// Decode Base64 PEM → Remove headers → Decode to DER → Import to RSA
var publicKeyBytes = Convert.FromBase64String(jwtSettings.PublicKey);
var publicKeyPem = Encoding.UTF8.GetString(publicKeyBytes);
var derKey = ConvertPemToDer(publicKeyPem);

var rsa = RSA.Create();
rsa.ImportSubjectPublicKeyInfo(derKey, out _);

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true
        };
    });
```

**Authorization Policies** (FR-029):
- `Customer`: `userType` claim = "customer"
- `Employee`: `userType` claim = "employee"
- `Manager`: `ClaimTypes.Role` = "Manager"
- `Admin`: `ClaimTypes.Role` = "Admin"
- `EmployeeOrHigher`: `userType` = "employee" OR role = "Manager" OR role = "Admin"

**Best Practices**:
- Validate issuer, audience, and lifetime on every token
- Use policy-based authorization (not role checks in controllers)
- Cache RSA public key (no need to recreate per request)

### 5. Validation

**Decision**: FluentValidation.DependencyInjectionExtensions 12.1.0

**Rationale**:
- FluentValidation.AspNetCore is DEPRECATED - must use DependencyInjectionExtensions
- Provides expressive, testable validation rules
- Integrates with ASP.NET Core model binding pipeline
- Supports complex validation scenarios (cross-field validation, async database lookups)

**Implementation Pattern**:
```csharp
// Startup
services.AddValidatorsFromAssemblyContaining<CreateRfqRequestValidator>();
services.AddScoped<IValidatorFactory, ServiceProviderValidatorFactory>();

// In controller action filter or manual invocation
public async Task<IActionResult> CreateRfq([FromBody] CreateRfqRequest request)
{
    var validator = new CreateRfqRequestValidator();
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return BadRequest(validationResult.Errors);
    // proceed...
}
```

**Best Practices**:
- One validator per request DTO
- Use `RuleFor()` with `.NotEmpty()`, `.EmailAddress()`, `.Must()` for custom rules
- Test validators independently with xUnit
- Use `.DependentRules()` for conditional validation chains

### 6. Observability & Logging

**Decision**: Serilog.AspNetCore 9.0.0 for structured logging, Prometheus.AspNetCore 8.2.1 for metrics, OpenTelemetry via Maliev.Aspire.ServiceDefaults for distributed tracing

**Rationale**:
- Serilog provides structured JSON logging to stdout (required by infrastructure)
- Prometheus metrics expose business and technical metrics (FR-034, FR-035)
- OpenTelemetry (via Aspire ServiceDefaults) enables distributed tracing across microservices
- Integrates with Google Cloud Logging and Monitoring without additional configuration

**Serilog Configuration**:
```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "Maliev.QuotationService")
    .Enrich.WithProperty("Version", Assembly.GetExecutingAssembly().GetName().Version)
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();
```

**Metrics Requirements** (FR-034, FR-035):
- **Business Metrics**: RFQ intake rate, quotation creation rate, conversion rates, average processing times, error rates
- **Technical Metrics**: API response times, external service call latencies, cache hit rates, resource utilization, circuit breaker state
- **Tags**: service_name, version, region, environment

**Best Practices**:
- Use `ILogger<T>` with structured logging: `logger.LogInformation("RFQ created: {RfqId}, Channel: {Channel}", rfqId, channel)`
- Emit custom metrics using Prometheus counters, gauges, histograms
- Distributed tracing spans for external HTTP calls, database queries, long-running operations
- Log correlation IDs to trace requests across services

### 7. Caching

**Decision**: StackExchange.Redis 2.10.1 with Microsoft.Extensions.Caching.StackExchangeRedis 10.0.0, with in-memory fallback for standalone development

**Rationale**:
- Redis provides distributed caching for material data, currency rates, customer matching results
- In-memory fallback (via `AddDistributedMemoryCache()`) for standalone development when Redis unavailable
- Supports cache expiration, sliding windows, and dependency tracking

**Configuration Pattern**:
```csharp
public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
{
    var settings = configuration.GetSection("Redis").Get<RedisSettings>() ?? new();

    if (settings.Enabled)
    {
        services.AddStackExchangeRedisCache(options =>
            options.Configuration = settings.ConnectionString);
    }
    else
    {
        services.AddDistributedMemoryCache(); // Fallback for standalone dev
    }
    return services;
}
```

**Caching Strategy**:
- **Material Data**: Cache for 1 hour (materials change infrequently)
- **Currency Rates**: Cache for 15 minutes (rates update periodically)
- **Customer Match Suggestions**: Cache for 5 minutes (ephemeral, session-based)

**Best Practices**:
- Use cache-aside pattern: check cache → if miss, fetch from service → store in cache
- Implement cache invalidation on Material Service updates (via event)
- Set appropriate TTL based on data volatility

### 8. Messaging

**Decision**: MassTransit.RabbitMQ 8.5.5 with in-memory fallback for standalone development

**Rationale**:
- RabbitMQ provides reliable message delivery for async operations (quotation PDF generation, audit log persistence)
- MassTransit simplifies message routing, retries, and error handling
- In-memory transport for standalone development when RabbitMQ unavailable

**Configuration Pattern**:
```csharp
public static IServiceCollection AddMassTransitWithRabbitMq(this IServiceCollection services, IConfiguration configuration)
{
    var settings = configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>() ?? new();

    if (settings.Enabled)
    {
        services.AddMassTransit(config =>
        {
            config.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(settings.Host, settings.VirtualHost, h =>
                {
                    h.Username(settings.Username);
                    h.Password(settings.Password);
                });
                cfg.ConfigureEndpoints(context);
            });
        });
    }
    else
    {
        services.AddMassTransit(config =>
        {
            config.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });
    }
    return services;
}
```

**Use Cases**:
- Publish `QuotationCreatedEvent` after quotation finalization
- Publish `RfqConvertedEvent` when RFQ becomes quotation
- Publish `AuditLogEvent` for async audit log persistence

**Best Practices**:
- Use message contracts (interfaces/classes) for type safety
- Configure retry policies (3 retries with exponential backoff)
- Use message headers for correlation IDs and user context

### 9. API Documentation

**Decision**: Scalar.AspNetCore 2.11.0 with Microsoft.AspNetCore.OpenApi 10.0.0

**Rationale**:
- Scalar provides modern, interactive API documentation UI
- OpenAPI specification enables contract-first development and client generation
- Scalar 2.x API requires 2-argument overload for `MapScalarApiReference`

**Routing Pattern** (CRITICAL):
```csharp
// Map OpenAPI at /quotation/openapi/v1.json
app.MapOpenApi("/quotation/openapi/{documentName}.json");

// Map Scalar at /quotation/scalar/v1 (Scalar 2.x API)
app.MapScalarApiReference("/quotation/scalar/v1", options =>
{
    options
        .WithTitle("Quotation Service API")
        .WithTheme(ScalarTheme.Default)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithOpenApiRoutePattern("/quotation/openapi/v1.json");
});

// Optional: Redirect root to Scalar
app.MapGet("/", () => Results.Redirect("/quotation/scalar/v1")).ExcludeFromDescription();
app.MapGet("/quotation", () => Results.Redirect("/quotation/scalar/v1")).ExcludeFromDescription();
```

**Best Practices**:
- FORBIDDEN: `app.UsePathBase()` - use explicit route prefixes instead
- Use `[Route("quotation/v{version:apiVersion}/rfqs")]` on controllers
- Document all request/response DTOs with XML comments
- Scalar UI available only in Development environment

### 10. Rate Limiting

**Decision**: ASP.NET Core 10.0 built-in `PartitionedRateLimiter`

**Rationale**:
- Native support in ASP.NET Core 10 without external dependencies
- Supports fixed window, sliding window, token bucket, and concurrency limiters
- Integrates with authorization and middleware pipeline

**Configuration**:
```csharp
services.AddRateLimiter(options =>
{
    // Global policy: 100 req/min per user
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Batch operations: 10 req/min
    options.AddPolicy("batch", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

**Middleware Order** (CRITICAL):
```csharp
app.UseHttpMetrics();        // Prometheus metrics
app.UseRateLimiter();        // Rate limiting
app.UseAuthentication();     // JWT authentication
app.UseAuthorization();      // Authorization policies
```

### 11. Testing Infrastructure

**Decision**: xUnit with Testcontainers 4.0.0+ (PostgreSQL, RabbitMQ, Redis modules)

**Rationale**:
- xUnit provides modern test framework with parallel execution, dependency injection, and fixtures
- Testcontainers ensures tests run against real infrastructure (Constitution Principle IV - NON-NEGOTIABLE)
- IntegrationTestWebAppFactory pattern manages container lifecycle with IAsyncLifetime

**Testcontainers Setup**:
```csharp
public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private readonly RabbitMqContainer _rabbitContainer;
    private readonly RedisContainer _redisContainer;

    public IntegrationTestWebAppFactory()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:18-alpine")
            .WithDatabase("test_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _rabbitContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:7.0")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _dbContainer.StartAsync(),
            _rabbitContainer.StartAsync(),
            _redisContainer.StartAsync()
        );
    }

    public new async Task DisposeAsync()
    {
        await Task.WhenAll(
            _dbContainer.DisposeAsync().AsTask(),
            _rabbitContainer.DisposeAsync().AsTask(),
            _redisContainer.DisposeAsync().AsTask()
        );
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Override DbContext with Testcontainers connection string
            services.RemoveAll<DbContextOptions<QuotationDbContext>>();
            services.AddDbContext<QuotationDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));

            // Apply migrations immediately
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<QuotationDbContext>();
            dbContext.Database.Migrate();
        });

        // Override RabbitMQ and Redis configuration
        builder.UseSetting("RabbitMQ:Host", _rabbitContainer.Hostname);
        builder.UseSetting("RabbitMQ:Port", _rabbitContainer.GetMappedPublicPort(5672).ToString());
        builder.UseSetting("Redis:ConnectionString", _redisContainer.GetConnectionString());
        builder.UseSetting("Redis:Enabled", "true");
    }
}
```

**Best Practices**:
- Use IClassFixture<IntegrationTestWebAppFactory> for integration tests
- Clean database between tests with manual DELETEs (not TRUNCATE due to MVCC)
- Mock external HTTP services (Currency, Material, Upload, PDF) in tests
- Test JWT authentication with TestAuthHandler providing admin claims

### 12. .NET Aspire Integration

**Decision**: Maliev.Aspire.ServiceDefaults 1.0.* consumed as NuGet package from GitHub Packages

**Rationale**:
- Each microservice has independent repository - project references fail in CI
- NuGet package provides shared observability standards (OpenTelemetry, health checks, service discovery)
- GitHub Packages enables private package hosting within organization

**Configuration**:

**nuget.config** (repository root):
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/MALIEV-Co-Ltd/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="%NUGET_USERNAME%" />
      <add key="ClearTextPassword" value="%NUGET_PASSWORD%" />
    </github>
  </packageSourceCredentials>
</configuration>
```

**Program.cs Integration**:
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Aspire ServiceDefaults for observability
builder.AddServiceDefaults();

// ... other service configuration

var app = builder.Build();

// Map Aspire default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

app.Run();
```

**CI/CD Authentication**:
```yaml
- name: Restore dependencies
  run: dotnet restore Maliev.QuotationService.sln
  env:
    NUGET_USERNAME: ${{ github.actor }}
    NUGET_PASSWORD: ${{ secrets.GITOPS_PAT }}  # NOT GITHUB_TOKEN
```

**Dockerfile BuildKit Secrets**:
```dockerfile
RUN --mount=type=secret,id=nuget_username \
    --mount=type=secret,id=nuget_password \
    NUGET_USERNAME=$(cat /run/secrets/nuget_username) \
    NUGET_PASSWORD=$(cat /run/secrets/nuget_password) \
    dotnet restore "./Maliev.QuotationService.Api/Maliev.QuotationService.Api.csproj"
```

**Best Practices**:
- Use PackageReference (NEVER ProjectReference) for ServiceDefaults
- CI workflows must use GITOPS_PAT (not GITHUB_TOKEN) for cross-repo package access
- Docker builds must use BuildKit secrets (NEVER ARG for credentials)

## Domain-Specific Best Practices

### Customer Matching Algorithm

**Decision**: Fuzzy string matching with weighted scoring

**Implementation Approach**:
1. Exact match on email (weight: 100)
2. Exact match on phone (weight: 90)
3. Levenshtein distance on name (weight: 70, threshold < 3 edits)
4. Sort suggestions by total score descending
5. Return top 5 matches with confidence percentage

**Libraries**: No external library needed - implement simple Levenshtein distance algorithm

### Quotation Versioning Strategy

**Decision**: Immutable versions with copy-on-write

**Implementation**:
- Each `Quotation` has `CurrentVersionNumber` foreign key to `QuotationVersion`
- Editing a quotation creates new `QuotationVersion` record
- Previous versions remain immutable for audit trail
- Version number increments automatically (v1, v2, v3...)
- `ChangeSummary` field captures what changed between versions

### State Machine for Quotation Lifecycle

**Decision**: Explicit state enum with transition validation

**States**:
```csharp
public enum QuotationStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    CustomerReview = 4,
    Accepted = 5,
    Expired = 6,
    Cancelled = 7
}
```

**Valid Transitions**:
- Draft → PendingApproval, CustomerReview, Cancelled
- PendingApproval → Approved, Draft, Cancelled
- Approved → CustomerReview, Cancelled
- CustomerReview → Accepted, Expired, Cancelled
- (Accepted, Expired, Cancelled are terminal states)

**Implementation**: Service layer validates transitions before persisting state changes

### RFQ Channel Metadata

**Decision**: Enum for known channels + extensibility for future channels

```csharp
public enum RfqChannel
{
    Website = 1,
    LINE = 2,
    WhatsApp = 3,
    FacebookMessenger = 4,
    Instagram = 5,
    Email = 6,
    InStore = 7,
    Unknown = 99  // For future channels not yet configured
}
```

### Data Retention & Archival

**Decision**: Soft delete with retention policy enforcement

**Implementation**:
- All entities have `IsDeleted` and `DeletedAt` fields
- Queries filter `IsDeleted = false` by default
- Background job (outside this service) purges data after 7 years
- Analytics queries support `IncludeDeleted` parameter for historical analysis

## Security Considerations

### Input Validation

- Validate all request DTOs with FluentValidation
- Sanitize customer input (names, notes) to prevent XSS
- Validate file references exist in Upload Service before storing
- Enforce maximum string lengths (e.g., customer name ≤ 200 chars)

### Authorization

- All endpoints require authentication except health checks
- RFQ/quotation creation: `[Authorize(Policy = "EmployeeOrHigher")]`
- Quotation approval: `[Authorize(Policy = "Manager")]`
- Analytics endpoints: `[Authorize(Policy = "EmployeeOrHigher")]`
- Customer linking: `[Authorize(Policy = "Employee")]`

### Secrets Management

- All secrets from Google Secret Manager mounted at `/mnt/secrets`
- Database connection string: `ConnectionStrings__QuotationDbContext`
- External service URLs: `ExternalServices__Currency__BaseUrl`, etc.
- JWT public key: `Jwt__PublicKey` (Base64-encoded PEM)
- RabbitMQ credentials: `RabbitMQ__Username`, `RabbitMQ__Password`
- Redis connection: `Redis__ConnectionString`

## Performance Optimizations

### Database Indexes

- **RFQs**: Index on `(ChannelSource, Status, CreatedAt)` for filtered queries
- **Quotations**: Index on `(Status, ValidityPeriodEnd, CreatedAt)` for expiration checks
- **Customers**: Unique index on `Email`, index on `PhoneNumber`, full-text index on `Name`
- **Audit Logs**: Index on `(EntityType, EntityId, Timestamp)` for audit trail queries

### Caching Strategy

- Cache material data for 1 hour (low volatility)
- Cache currency rates for 15 minutes (moderate volatility)
- Cache customer match results for 5 minutes (session-based)
- Invalidate caches on relevant events from external services

### Query Optimization

- Use `AsNoTracking()` for read-only queries
- Eager load related entities with `.Include()` to avoid N+1 queries
- Project to DTOs in database query to reduce data transfer
- Use pagination for list endpoints (default page size: 20, max: 100)

## Deployment Considerations

### Docker

- Multi-stage build: SDK for build, ASP.NET runtime for final image
- Built-in `app` user (no custom user creation)
- Health check endpoint: `/quotation/liveness`
- Single port 8080 exposed
- Environment variables for all configuration

### Kubernetes (future)

- Horizontal Pod Autoscaler based on CPU and request metrics
- ConfigMaps for non-secret configuration
- Secrets from Google Secret Manager via external-secrets-operator
- Liveness probe: `/quotation/liveness`
- Readiness probe: `/quotation/readiness`

## Conclusion

All technology decisions align with MALIEV microservices constitution and technical standards. The service leverages .NET 10, Entity Framework Core, PostgreSQL, Redis, RabbitMQ, and comprehensive observability tooling to deliver a robust, scalable, and maintainable quotation management system. Implementation will follow Test-First Development with Testcontainers ensuring production fidelity in all test scenarios.

**Next Phase**: Proceed to Phase 1 - Data Model & API Contract Design
