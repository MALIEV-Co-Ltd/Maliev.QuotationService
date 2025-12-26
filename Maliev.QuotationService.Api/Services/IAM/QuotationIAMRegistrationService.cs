using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Maliev.QuotationService.Api.Services.IAM;

/// <summary>
/// Background service that registers Quotation Service permissions and roles with the central IAM service on startup.
/// </summary>
public class QuotationIAMRegistrationService : IHostedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QuotationIAMRegistrationService> _logger;

    public QuotationIAMRegistrationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<QuotationIAMRegistrationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting IAM registration for QuotationService...");

        try
        {
            using var client = _httpClientFactory.CreateClient("IAM");
            var baseUrl = client.BaseAddress?.ToString() ?? _configuration["ExternalServices:IAM:BaseUrl"];

            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogWarning("IAM BaseUrl not configured. Skipping IAM registration.");
                return;
            }

            if (client.BaseAddress == null)
            {
                client.BaseAddress = new Uri(baseUrl);
            }

            // Add service account token if configured
            var token = _configuration["ExternalServices:IAM:ServiceAccountToken"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            // 1. Register Permissions
            var permissions = QuotationPermissions.GetPermissions().Select(p => new
            {
                PermissionId = p.Id,
                Description = p.Description
            }).ToList();

            var permRequest = new
            {
                ServiceName = "quotation",
                Permissions = permissions
            };

            _logger.LogDebug("Registering {Count} permissions with IAM at {BaseUrl}", permissions.Count, baseUrl);
            var permResponse = await client.PostAsJsonAsync("iam/v1/permissions/register", permRequest, cancellationToken);
            
            if (permResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully registered {Count} permissions with IAM.", permissions.Count);
            }
            else
            {
                var error = await permResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to register permissions with IAM. Status: {StatusCode}, Error: {Error}", 
                    permResponse.StatusCode, error);
            }

            // 2. Register Roles
            var roles = QuotationPredefinedRoles.GetRoles().Select(r => new
            {
                RoleId = r.Name,
                Description = r.Description,
                PermissionIds = r.Permissions.ToList()
            }).ToList();

            var roleRequest = new
            {
                ServiceName = "quotation",
                Roles = roles
            };

            _logger.LogDebug("Registering {Count} roles with IAM at {BaseUrl}", roles.Count, baseUrl);
            var roleResponse = await client.PostAsJsonAsync("iam/v1/roles/register", roleRequest, cancellationToken);
            
            if (roleResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully registered {Count} roles with IAM.", roles.Count);
            }
            else
            {
                var error = await roleResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to register roles with IAM. Status: {StatusCode}, Error: {Error}", 
                    roleResponse.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during IAM registration for QuotationService.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
