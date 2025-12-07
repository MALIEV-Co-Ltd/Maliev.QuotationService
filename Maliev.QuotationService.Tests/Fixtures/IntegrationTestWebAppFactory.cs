using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Maliev.QuotationService.Api.Configuration.Settings;
using Maliev.QuotationService.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace Maliev.QuotationService.Tests.Fixtures;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;
    private readonly RedisContainer _redisContainer;
    private readonly RSA _testRsa;
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";

    public IntegrationTestWebAppFactory()
    {
        // Generate ephemeral RSA key for test JWT tokens
        _testRsa = RSA.Create(2048);

        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:18")
            .WithDatabase("quotation_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _rabbitMqContainer = new RabbitMqBuilder()
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
        // Start all containers in parallel for faster test startup
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _rabbitMqContainer.StartAsync(),
            _redisContainer.StartAsync()
        );
    }

    public new async Task DisposeAsync()
    {
        // Stop all containers in parallel
        await Task.WhenAll(
            _postgresContainer.DisposeAsync().AsTask(),
            _rabbitMqContainer.DisposeAsync().AsTask(),
            _redisContainer.DisposeAsync().AsTask()
        );

        _testRsa.Dispose();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            services.RemoveAll<DbContextOptions<QuotationDbContext>>();
            services.RemoveAll<QuotationDbContext>();

            // Mock MaterialService
            services.RemoveAll<Maliev.QuotationService.Api.ExternalClients.Interfaces.IMaterialServiceClient>();
            services.AddSingleton<Maliev.QuotationService.Api.ExternalClients.Interfaces.IMaterialServiceClient, Maliev.QuotationService.Tests.Mocks.MockMaterialServiceClient>();

            // Add DbContext with Testcontainers connection string
            services.AddDbContext<QuotationDbContext>(options =>
            {
                options.UseNpgsql(_postgresContainer.GetConnectionString());
            });

            // Override RabbitMQ settings to use Testcontainers
            services.Configure<RabbitMQSettings>(settings =>
            {
                settings.Enabled = true;
                settings.Host = _rabbitMqContainer.Hostname;
                settings.Port = (ushort)_rabbitMqContainer.GetMappedPublicPort(5672);
                settings.VirtualHost = "/";
                settings.Username = "guest";
                settings.Password = "guest";
            });

            // Override Redis settings to use Testcontainers
            services.Configure<RedisSettings>(settings =>
            {
                settings.Enabled = true;
                settings.ConnectionString = _redisContainer.GetConnectionString();
            });

            // Configure JWT settings with ephemeral test key
            // We'll reconfigure authentication entirely in test environment
            services.Configure<JwtSettings>(settings =>
            {
                settings.PublicKey = string.Empty; // Empty = test mode
                settings.Issuer = TestIssuer;
                settings.Audience = TestAudience;
            });

            // PostConfigure JWT Bearer options to use our test RSA key
            services.PostConfigureAll<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = TestIssuer,
                    ValidAudience = TestAudience,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(_testRsa),
                    ClockSkew = TimeSpan.Zero // No clock skew for tests
                };
            });

            // Build service provider and apply migrations
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<QuotationDbContext>();
            dbContext.Database.Migrate();
        });
    }

    /// <summary>
    /// Creates a test JWT token with specified claims for integration testing.
    /// </summary>
    /// <param name="userId">User ID claim</param>
    /// <param name="roles">User roles</param>
    /// <param name="additionalClaims">Additional claims to include</param>
    /// <returns>JWT token string</returns>
    public string CreateTestJwtToken(string userId = "test-user", string[]? roles = null, Dictionary<string, string>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add roles
        roles ??= new[] { "Employee" };
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add additional claims
        if (additionalClaims != null)
        {
            foreach (var (key, value) in additionalClaims)
            {
                claims.Add(new Claim(key, value));
            }
        }

        var credentials = new SigningCredentials(
            new RsaSecurityKey(_testRsa),
            SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
