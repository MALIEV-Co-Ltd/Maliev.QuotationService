using Maliev.QuotationService.Data;
using Maliev.MessagingContracts.Contracts.Customers;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Maliev.QuotationService.Api.Consumers;

/// <summary>
/// Consumes CustomerDeletedEvent to sync customer soft-deletes to local Quotation Service database.
/// </summary>
public class CustomerDeletedEventConsumer : IConsumer<CustomerDeletedEvent>
{
    private readonly QuotationDbContext _dbContext;
    private readonly ILogger<CustomerDeletedEventConsumer> _logger;

    public CustomerDeletedEventConsumer(QuotationDbContext dbContext, ILogger<CustomerDeletedEventConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CustomerDeletedEvent> context)
    {
        var message = context.Message;
        var payload = message.Payload;

        _logger.LogInformation("Processing CustomerDeletedEvent for CustomerId: {CustomerId}", payload.CustomerId);

        var customer = await _dbContext.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == payload.CustomerId);

        if (customer == null)
        {
            _logger.LogWarning("Customer {CustomerId} not found in local database for deletion.", payload.CustomerId);
            return;
        }

        customer.IsDeleted = true;
        customer.DeletedAt = payload.DeletedAt.UtcDateTime;
        customer.UpdatedAt = payload.DeletedAt.UtcDateTime;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Successfully soft-deleted customer {CustomerId} in local database.", payload.CustomerId);
    }
}
