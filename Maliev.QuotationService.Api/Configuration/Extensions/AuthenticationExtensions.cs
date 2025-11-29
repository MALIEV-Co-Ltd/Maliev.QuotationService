using System.Security.Cryptography;
using Maliev.QuotationService.Api.Configuration.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Maliev.QuotationService.Api.Configuration.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>();

        // For testing environments, generate ephemeral RSA key if not configured
        var rsa = RSA.Create();

        if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.PublicKey))
        {
            // Generate ephemeral 2048-bit RSA key for testing
            rsa = RSA.Create(2048);

            // Use default test issuer/audience if not configured
            jwtSettings ??= new JwtSettings
            {
                Issuer = "test-issuer",
                Audience = "test-audience",
                PublicKey = string.Empty
            };
        }
        else
        {
            // Production: Import actual public key
            try
            {
                var publicKeyBytes = Convert.FromBase64String(jwtSettings.PublicKey);
                rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
            }
            catch
            {
                // If Base64 decode fails, try importing as PEM directly
                rsa.ImportFromPem(jwtSettings.PublicKey);
            }
        }

        var rsaSecurityKey = new RsaSecurityKey(rsa);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = rsaSecurityKey
                };
            });

        return services;
    }

    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Customer", policy => policy.RequireRole("Customer"));
            options.AddPolicy("Employee", policy => policy.RequireRole("Employee"));
            options.AddPolicy("Manager", policy => policy.RequireRole("Manager"));
            options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
            options.AddPolicy("EmployeeOrHigher", policy =>
                policy.RequireRole("Employee", "Manager", "Admin"));
        });

        return services;
    }
}
