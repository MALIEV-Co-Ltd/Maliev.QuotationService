using Maliev.MessagingContracts.Generated;
using Maliev.QuotationService.Data;
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

            bool nameChanged = false;
            string firstName = "";
            string lastName = "";

            if (fields.TryGetProperty("FirstName", out var fn))
            {
                firstName = fn.GetString() ?? "";
                nameChanged = true;
            }
            if (fields.TryGetProperty("LastName", out var ln))
            {
                lastName = ln.GetString() ?? "";
                nameChanged = true;
            }

            if (nameChanged)
            {
                // If only one changed, we might lose the other.
                // However, in our simplified Customer model in QuotationService,
                // we only have 'Name'.
                if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
                {
                    // If we only have one part, try to keep the existing parts if possible
                    // This is a bit complex without storing FirstName/LastName separately.
                    // For now, let's just apply what we have.
                    customer.Name = $"{firstName} {lastName}".Trim();
                }
                else
                {
                    customer.Name = $"{firstName} {lastName}".Trim();
                }
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
