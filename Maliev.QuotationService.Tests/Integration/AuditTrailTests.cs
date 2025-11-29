using System.Net.Http.Json;
using FluentAssertions;
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
        rfqResponse.Should().NotBeNull();

        // Verify audit log entry was created
        var auditEntries = await DbContext.AuditLogEntries
            .Where(a => a.EntityType == AuditEntityType.RFQ && a.EntityId == rfqResponse!.Id)
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

        auditEntries.Should().NotBeEmpty();

        var createEntry = auditEntries.FirstOrDefault(a => a.ActionType == AuditActionType.Create);
        createEntry.Should().NotBeNull();
        createEntry!.EntityId.Should().Be(rfqResponse!.Id);
        createEntry.EntityType.Should().Be(AuditEntityType.RFQ);
        createEntry.ActionType.Should().Be(AuditActionType.Create);
        createEntry.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // ChangedFields should contain the initial data
        createEntry.ChangedFields.Should().NotBeNull();
    }
}
