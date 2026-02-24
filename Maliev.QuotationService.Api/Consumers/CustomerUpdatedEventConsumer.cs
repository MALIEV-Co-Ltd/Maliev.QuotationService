using Maliev.QuotationService.Data;
using Maliev.MessagingContracts.Contracts.Customers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Maliev.QuotationService.Api.Consumers;

/// <summary>
/// Consumes CustomerUpdatedEvent to sync customer changes to local Quotation Service database.
/// </summary>
public class CustomerUpdatedEventConsumer : IConsumer<CustomerUpdatedEvent>
{
    private readonly QuotationDbContext _dbContext;
    private readonly ILogger<CustomerUpdatedEventConsumer> _logger;

    public CustomerUpdatedEventConsumer(QuotationDbContext dbContext, ILogger<CustomerUpdatedEventConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CustomerUpdatedEvent> context)
    {
        var message = context.Message;
        var payload = message.Payload;

        _logger.LogInformation("Processing CustomerUpdatedEvent for CustomerId: {CustomerId}", payload.CustomerId);

        var customer = await _dbContext.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == payload.CustomerId);

        if (customer == null)
        {
            _logger.LogWarning("Customer {CustomerId} not found in local database for update. This might happen if the created event was missed or delayed.", payload.CustomerId);
            // In a production scenario, we might want to fetch the latest state from CustomerService here (lazy load)
            return;
        }

        // Apply changed fields
        if (payload.UpdatedFields != null)
        {
            var fields = JsonSerializer.SerializeToElement(payload.UpdatedFields);

            bool hasFirstName = fields.TryGetProperty("FirstName", out var fn);
            bool hasLastName = fields.TryGetProperty("LastName", out var ln);

            if (hasFirstName || hasLastName)
            {
                // We parse the existing name to preserve the unchanged portion.
                var existingParts = (customer.Name ?? "").Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var existingFirstName = existingParts.Length > 0 ? existingParts[0] : "";
                var existingLastName = existingParts.Length > 1 ? existingParts[1] : "";

                var finalFirstName = hasFirstName ? (fn.GetString() ?? "") : existingFirstName;
                var finalLastName = hasLastName ? (ln.GetString() ?? "") : existingLastName;

                customer.Name = $"{finalFirstName} {finalLastName}".Trim();
            }

            if (fields.TryGetProperty("Email", out var email))
            {
                customer.Email = email.GetString();
            }

            if (fields.TryGetProperty("Mobile", out var mobile))
            {
                customer.PhoneNumber = mobile.GetString();
            }
            else if (fields.TryGetProperty("Landline", out var landline))
            {
                customer.PhoneNumber = landline.GetString();
            }
        }

        customer.UpdatedAt = payload.UpdatedAt.UtcDateTime;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Successfully updated customer {CustomerId} in local database.", payload.CustomerId);
    }
}
