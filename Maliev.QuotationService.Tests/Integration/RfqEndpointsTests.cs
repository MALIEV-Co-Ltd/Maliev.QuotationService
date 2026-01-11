using System.Net;
using System.Net.Http.Json;
using Xunit;
using Maliev.QuotationService.Api.DTOs.Requests;
using Maliev.QuotationService.Api.DTOs.Responses;
using Maliev.QuotationService.Data.Entities;
using Maliev.QuotationService.Data.Enums;
using Maliev.QuotationService.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Maliev.QuotationService.Tests.Integration;

public class RfqEndpointsTests : BaseIntegrationTest
{
    public RfqEndpointsTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateRfq_ValidRequest_ReturnsCreated()
    {
        // Arrange
        using var authenticatedClient = CreateAuthenticatedClient();
        var request = new CreateRfqRequest
        {
            CustomerEmail = "newcustomer@example.com",
            CustomerName = "New Customer",
            CustomerPhoneNumber = "+66987654321",
            ChannelSource = RfqChannel.Website,
            RequestDetails = new { description = "I need a quote for custom parts", quantity = 100 }
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/quotation/v1/rfqs", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var rfqResponse = await response.Content.ReadFromJsonAsync<RfqResponse>();
        Assert.NotNull(rfqResponse);
        Assert.NotEqual(Guid.Empty, rfqResponse.Id);
        Assert.NotNull(rfqResponse.Customer);
        Assert.Equal("newcustomer@example.com", rfqResponse.Customer.Email);
        Assert.Equal(RfqChannel.Website, rfqResponse.ChannelSource);
        Assert.Equal(RfqStatus.New, rfqResponse.Status);

        // Verify database persistence
        var savedRfq = await DbContext.Rfqs
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == rfqResponse.Id);

        Assert.NotNull(savedRfq);
        Assert.Equal("newcustomer@example.com", savedRfq.Customer.Email);
    }

    [Fact]
    public async Task GetRfqs_WithFilters_ReturnsFilteredResults()
    {
        // Arrange - Create test data
        var customer1 = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "customer1@example.com",
            Name = "Customer One",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var customer2 = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "customer2@example.com",
            Name = "Customer Two",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var rfq1 = new Rfq
        {
            Id = Guid.NewGuid(),
            CustomerId = customer1.Id,
            ChannelSource = RfqChannel.Website,
            Status = RfqStatus.New,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var rfq2 = new Rfq
        {
            Id = Guid.NewGuid(),
            CustomerId = customer2.Id,
            ChannelSource = RfqChannel.LINE,
            Status = RfqStatus.InProgress,
            AssignedStaffUserId = "staff-123",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var rfq3 = new Rfq
        {
            Id = Guid.NewGuid(),
            CustomerId = customer1.Id,
            ChannelSource = RfqChannel.Email,
            Status = RfqStatus.Converted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Customers.AddRange(customer1, customer2);
        DbContext.Rfqs.AddRange(rfq1, rfq2, rfq3);
        await DbContext.SaveChangesAsync();

        using var authenticatedClient = CreateAuthenticatedClient();

        // Act - Filter by channel
        var response1 = await authenticatedClient.GetAsync("/quotation/v1/rfqs?channel=Website");
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        var result1 = await response1.Content.ReadFromJsonAsync<PagedResponse<RfqResponse>>();
        Assert.NotNull(result1);
        Assert.Single(result1.Data);
        Assert.Equal(RfqChannel.Website, result1.Data.First().ChannelSource);

        // Act - Filter by status
        var response2 = await authenticatedClient.GetAsync("/quotation/v1/rfqs?status=InProgress");
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var result2 = await response2.Content.ReadFromJsonAsync<PagedResponse<RfqResponse>>();
        Assert.NotNull(result2);
        Assert.Single(result2.Data);
        Assert.Equal(RfqStatus.InProgress, result2.Data.First().Status);

        // Act - Filter by assigned staff
        var response3 = await authenticatedClient.GetAsync("/quotation/v1/rfqs?assignedStaffUserId=staff-123");
        Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
        var result3 = await response3.Content.ReadFromJsonAsync<PagedResponse<RfqResponse>>();
        Assert.NotNull(result3);
        Assert.Single(result3.Data);
        Assert.Equal("staff-123", result3.Data.First().AssignedStaffUserId);
    }

    [Fact]
    public async Task AddNote_ValidNote_AddsToRfq()
    {
        // Arrange - Create test RFQ
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "customer@example.com",
            Name = "Test Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var rfq = new Rfq
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            ChannelSource = RfqChannel.WhatsApp,
            Status = RfqStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Customers.Add(customer);
        DbContext.Rfqs.Add(rfq);
        await DbContext.SaveChangesAsync();

        var noteRequest = new AddInternalNoteRequest
        {
            Content = "Customer requested urgent delivery"
        };

        using var authenticatedClient = CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.PostAsJsonAsync($"/quotation/v1/rfqs/{rfq.Id}/notes", noteRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var noteResponse = await response.Content.ReadFromJsonAsync<InternalNoteResponse>();
        Assert.NotNull(noteResponse);
        Assert.Equal("Customer requested urgent delivery", noteResponse.Content);
        Assert.True(DateTime.UtcNow.Subtract(noteResponse.CreatedAt).TotalSeconds < 5);

        // Verify database persistence
        var savedNote = await DbContext.InternalNotes
            .FirstOrDefaultAsync(n => n.RfqId == rfq.Id);

        Assert.NotNull(savedNote);
        Assert.Equal("Customer requested urgent delivery", savedNote.Content);
    }
}
