using System.Net.Http.Json;
using Maliev.QuotationService.Api.DTOs.Requests;
using Maliev.QuotationService.Api.DTOs.Responses;
using Maliev.QuotationService.Data.Enums;
using Maliev.QuotationService.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Maliev.QuotationService.Tests.Integration;

public class AuditTrailTests : BaseIntegrationTest
{
    public AuditTrailTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateRfq_CreatesAuditLogEntry()
    {
        // Arrange
        using var authenticatedClient = CreateAuthenticatedClient();
        var request = new CreateRfqRequest
        {
            CustomerEmail = "auditcustomer@example.com",
            CustomerName = "Audit Customer",
            CustomerPhoneNumber = "+66111222333",
            ChannelSource = RfqChannel.LINE,
            RequestDetails = new { description = "Test RFQ for audit trail" }
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/quotation/v1/rfqs", request);
        var rfqResponse = await response.Content.ReadFromJsonAsync<RfqResponse>();

        // Assert
        Assert.NotNull(rfqResponse);

        // Verify audit log entry was created
        var auditEntries = await DbContext.AuditLogEntries
            .Where(a => a.EntityType == AuditEntityType.RFQ && a.EntityId == rfqResponse!.Id)
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

        Assert.NotEmpty(auditEntries);

        var createEntry = auditEntries.FirstOrDefault(a => a.ActionType == AuditActionType.Create);
        Assert.NotNull(createEntry);
        Assert.Equal(rfqResponse!.Id, createEntry!.EntityId);
        Assert.Equal(AuditEntityType.RFQ, createEntry.EntityType);
        Assert.Equal(AuditActionType.Create, createEntry.ActionType);
        Assert.InRange(createEntry.Timestamp, DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));

        // ChangedFields should contain the initial data
        Assert.NotNull(createEntry.ChangedFields);
    }
}
