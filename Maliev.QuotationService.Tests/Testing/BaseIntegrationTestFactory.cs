using System.IdentityModel.Tokens.Jwt;
using System.Diagnostics.CodeAnalysis;
using MassTransit;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace Maliev.QuotationService.Tests.Testing;

/// <summary>
/// Base integration test factory for QuotationService.
/// Provides PostgreSQL, Redis, and RabbitMQ containers with parallel startup.
/// </summary>
/// <typeparam name="TProgram">The Program class of the service being tested</typeparam>
/// <typeparam name="TDbContext">The DbContext type for the service</typeparam>
public class BaseIntegrationTestFactory<TProgram, TDbContext> : WebApplicationFactory<TProgram>, IAsyncLifetime
    where TProgram : class
    where TDbContext : DbContext
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RedisContainer _redisContainer;
    private readonly RabbitMqContainer _rabbitmqContainer;
    private readonly RSA _testRsa;
    private bool _containersStarted;

    /// <summary>
    /// Override this property if your DbContext connection string has a different name.
    /// Defaults to the DbContext class name.
    /// </summary>
    protected virtual string DbConnectionStringName => typeof(TDbContext).Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseIntegrationTestFactory{TProgram, TDbContext}"/> class.
    /// Sets up the container builders for PostgreSQL, Redis, and RabbitMQ.
    /// </summary>
    public BaseIntegrationTestFactory()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:18-alpine")
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:8.4-alpine")
            .Build();

        _rabbitmqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:4.2-alpine")
            .Build();

        _testRsa = RSA.Create(2048);

        // Set environment variable EARLY so Program.cs picks it up during WebApplication.CreateBuilder
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    /// <summary>
    /// Starts the Docker containers and applies database migrations.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_containersStarted)
            return;

        // Start all containers in parallel
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _redisContainer.StartAsync(),
            _rabbitmqContainer.StartAsync()
        );

        // Set environment variables immediately after containers start
        Environment.SetEnvironmentVariable($"ConnectionStrings__{DbConnectionStringName}", _postgresContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__redis", _redisContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__rabbitmq", _rabbitmqContainer.GetConnectionString());

        // Wait for Redis to be ready (with light error handling for CI stability)
        try
        {
            using (var connection = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString()))
            {
                await connection.GetDatabase().PingAsync();
            }
        }
        catch (Exception)
        {
            throw;
        }

        // Apply database migrations
        await ApplyMigrationsAsync();

        _containersStarted = true;
    }

    /// <summary>
    /// Disposes of the Docker containers and cleans up environment variables.
    /// </summary>
    public new async Task DisposeAsync()
    {
        // Explicitly stop MassTransit bus if it was started
        if (Services != null)
        {
            var busControl = Services.GetService<IBusControl>();
            if (busControl != null)
            {
                await busControl.StopAsync();
            }
        }

        await _postgresContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
        await _rabbitmqContainer.DisposeAsync();
        _testRsa.Dispose();
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        await base.DisposeAsync();
    }

    /// <inheritdoc />
    protected override IHost CreateHost(IHostBuilder builder)
    {
        if (!_containersStarted)
        {
            InitializeAsync().GetAwaiter().GetResult();
        }

        var rsaParams = _testRsa.ExportParameters(false);
        Environment.SetEnvironmentVariable("JWT_PUBLIC_KEY_MODULUS", Convert.ToBase64String(rsaParams.Modulus!));
        Environment.SetEnvironmentVariable("JWT_PUBLIC_KEY_EXPONENT", Convert.ToBase64String(rsaParams.Exponent!));

        ConfigureEnvironmentVariables();

        var host = base.CreateHost(builder);

        using (var scope = host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        }

        return host;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecurityKey"] = "test-secret-key-at-least-32-characters-long"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.PostConfigureAll<JwtBearerOptions>(options =>
            {
                // Disable claim type mapping to keep original claim names
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "test-issuer",
                    ValidAudience = "test-audience",
                    IssuerSigningKey = new RsaSecurityKey(_testRsa),
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = JwtRegisteredClaimNames.Sub, // Use "sub" claim as name identifier
                    RoleClaimType = "role" // Use "role" claim for roles
                };

                // Add event to transform claims after token validation
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            // Add ClaimTypes.NameIdentifier claim from "sub"
                            var subClaim = identity.FindFirst(JwtRegisteredClaimNames.Sub);
                            if (subClaim != null && !identity.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subClaim.Value));
                            }

                            // Add ClaimTypes.Role claims from "role"
                            var roleClaims = identity.FindAll("role").ToList();
                            foreach (var roleClaim in roleClaims)
                            {
                                if (!identity.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == roleClaim.Value))
                                {
                                    identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
                                }
                            }
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // Add MassTransit test harness for testing message publishing/consuming
            services.AddMassTransitTestHarness();

            ConfigureAdditionalServices(services);
        });
    }

    /// <summary>
    /// Override this method to set additional environment variables before host creation.
    /// </summary>
    protected virtual void ConfigureEnvironmentVariables() { }

    /// <summary>
    /// Override this method to add additional test services to the DI container.
    /// </summary>
    /// <param name="services">The service collection</param>
    protected virtual void ConfigureAdditionalServices(IServiceCollection services) { }

    /// <summary>
    /// Gets the DbContext from the service provider for use in tests.
    /// </summary>
    /// <returns>The DbContext instance</returns>
    public TDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<TDbContext>();
    }

    /// <summary>
    /// Creates a new DbContext instance for testing (not from DI container).
    /// </summary>
    /// <returns>A new DbContext instance</returns>
    public TDbContext CreateDbContext()
    {
        var connectionString = _postgresContainer.GetConnectionString();
        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
            npgsqlOptions.MigrationsAssembly(typeof(TDbContext).Assembly.GetName().Name));
        return (TDbContext)Activator.CreateInstance(typeof(TDbContext), optionsBuilder.Options)!;
    }

    /// <summary>
    /// Applies all pending migrations to the test database.
    /// </summary>
    private async Task ApplyMigrationsAsync()
    {
        try
        {
            await using var context = CreateDbContext();
            await context.Database.MigrateAsync();

            // Verify tables exist
            var tables = await context.Database
                .SqlQueryRaw<string>("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'")
                .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Cleans all data from the database while preserving schema.
    /// Queries the database schema dynamically to get all tables.
    /// </summary>
    [SuppressMessage("Security", "EF1002:Gaps in SQL queries", Justification = "Table names are retrieved from information_schema and are safe.")]
    public async Task CleanDatabaseAsync()
    {
        await using var context = CreateDbContext();

        var tableNames = await context.Database
            .SqlQueryRaw<string>(
                @"SELECT table_name
                  FROM information_schema.tables
                  WHERE table_schema = 'public'
                  AND table_type = 'BASE TABLE'
                  AND table_name != '__EFMigrationsHistory'
                  ORDER BY table_name")
            .ToListAsync();

        foreach (var tableName in tableNames)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{tableName}\" RESTART IDENTITY CASCADE");
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01") { }
        }
    }

    /// <summary>
    /// Resets the database state. Alias for <see cref="CleanDatabaseAsync"/>.
    /// </summary>
    public Task ResetDatabaseAsync() => CleanDatabaseAsync();

    /// <summary>
    /// Clears the database state. Alias for <see cref="CleanDatabaseAsync"/>.
    /// </summary>
    public Task ClearDatabaseAsync() => CleanDatabaseAsync();

    /// <summary>
    /// Clears the in-memory cache.
    /// </summary>
    public void ClearCache()
    {
        var memoryCache = Services.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
        if (memoryCache is Microsoft.Extensions.Caching.Memory.MemoryCache cache)
        {
            cache.Compact(1.0);
        }
    }

    /// <summary>
    /// Gets the RSA signing credentials for JWT token creation.
    /// </summary>
    public SigningCredentials SigningCredentials => new SigningCredentials(new RsaSecurityKey(_testRsa), SecurityAlgorithms.RsaSha256);

    /// <summary>
    /// Creates a test JWT token for authentication in integration tests.
    /// </summary>
    /// <param name="userId">User ID to include in token</param>
    /// <param name="roles">Roles to include in token claims</param>
    /// <param name="additionalClaims">Additional claims to include</param>
    /// <returns>JWT token string</returns>
    public string CreateTestJwtToken(
        string userId = "test-user",
        string[]? roles = null,
        IEnumerable<Claim>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var effectiveRoles = roles ?? new[] { "Employee" };
        foreach (var role in effectiveRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims);
        }

        var rsaSecurityKey = new RsaSecurityKey(_testRsa);
        var signingCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Simplified JWT token generator with role parameter.
    /// </summary>
    /// <param name="userId">User ID to include in token</param>
    /// <param name="role">User role</param>
    /// <returns>JWT token string</returns>
    public string GenerateTestToken(string userId = "test-user", string role = "admin")
    {
        return CreateTestJwtToken(userId, new[] { role });
    }

    /// <summary>
    /// Creates a test JWT token for authentication in integration tests (Legacy support).
    /// </summary>
    public string CreateTestJwtToken(
        string userId,
        string[]? roles,
        Dictionary<string, string>? additionalClaims)
    {
        var claims = additionalClaims?.Select(kv => new Claim(kv.Key, kv.Value));
        return CreateTestJwtToken(userId, roles, claims);
    }

    /// <summary>
    /// Creates an HTTP client with authenticated user and specified roles.
    /// </summary>
    /// <param name="userId">User ID for the token</param>
    /// <param name="roles">User roles</param>
    /// <returns>HttpClient with Authorization header set</returns>
    public HttpClient CreateAuthenticatedClient(string userId = "test-user", string[]? roles = null)
    {
        var token = CreateTestJwtToken(userId, roles);
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }
}
