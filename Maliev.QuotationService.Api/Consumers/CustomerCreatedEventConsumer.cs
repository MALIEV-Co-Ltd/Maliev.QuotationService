using Maliev.MessagingContracts.Generated;
using Maliev.QuotationService.Data;
using Maliev.QuotationService.Data.Entities;
using Maliev.MessagingContracts.Contracts.Customers;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Maliev.QuotationService.Api.Consumers;

/// <summary>
/// Consumes CustomerCreatedEvent to sync customer data to local Quotation Service database.
/// </summary>
public class CustomerCreatedEventConsumer : IConsumer<CustomerCreatedEvent>
{
    private readonly QuotationDbContext _dbContext;
    private readonly ILogger<CustomerCreatedEventConsumer> _logger;

    public CustomerCreatedEventConsumer(QuotationDbContext dbContext, ILogger<CustomerCreatedEventConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CustomerCreatedEvent> context)
    {
        var message = context.Message;
        var payload = message.Payload;

        _logger.LogInformation("Processing CustomerCreatedEvent for CustomerId: {CustomerId}", payload.CustomerId);

        var existingCustomer = await _dbContext.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == payload.CustomerId);

        if (existingCustomer != null)
        {
            _logger.LogWarning("Customer {CustomerId} already exists in local database. Updating instead of creating.", payload.CustomerId);
            UpdateCustomer(existingCustomer, payload);
        }
        else
        {
            var customer = new Customer
            {
                Id = payload.CustomerId,
                Name = $"{payload.FirstName} {payload.LastName}".Trim(),
                Email = payload.Email,
                PhoneNumber = payload.Mobile ?? payload.Landline,
                CreatedAt = payload.CreatedAt.UtcDateTime,
                UpdatedAt = payload.CreatedAt.UtcDateTime,
                IsDeleted = false
            };

            _dbContext.Customers.Add(customer);
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Successfully synced customer {CustomerId} to local database.", payload.CustomerId);
    }

    private static void UpdateCustomer(Customer customer, CustomerCreatedEventPayload payload)
    {
        customer.Name = $"{payload.FirstName} {payload.LastName}".Trim();
        customer.Email = payload.Email;
        customer.PhoneNumber = payload.Mobile ?? payload.Landline;
        customer.UpdatedAt = payload.CreatedAt.UtcDateTime;
        customer.IsDeleted = false;
        customer.DeletedAt = null;
    }
}
