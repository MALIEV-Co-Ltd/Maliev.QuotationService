using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
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
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var rfqResponse = await response.Content.ReadFromJsonAsync<RfqResponse>();
        rfqResponse.Should().NotBeNull();
        rfqResponse!.Id.Should().NotBeEmpty();
        rfqResponse.Customer.Should().NotBeNull();
        rfqResponse.Customer.Email.Should().Be("newcustomer@example.com");
        rfqResponse.ChannelSource.Should().Be(RfqChannel.Website);
        rfqResponse.Status.Should().Be(RfqStatus.New);

        // Verify database persistence
        var savedRfq = await DbContext.Rfqs
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == rfqResponse.Id);

        savedRfq.Should().NotBeNull();
        savedRfq!.Customer.Email.Should().Be("newcustomer@example.com");
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
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var result1 = await response1.Content.ReadFromJsonAsync<List<RfqResponse>>();
        result1.Should().HaveCount(1);
        result1![0].ChannelSource.Should().Be(RfqChannel.Website);

        // Act - Filter by status
        var response2 = await authenticatedClient.GetAsync("/quotation/v1/rfqs?status=InProgress");
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var result2 = await response2.Content.ReadFromJsonAsync<List<RfqResponse>>();
        result2.Should().HaveCount(1);
        result2![0].Status.Should().Be(RfqStatus.InProgress);

        // Act - Filter by assigned staff
        var response3 = await authenticatedClient.GetAsync("/quotation/v1/rfqs?assignedStaffUserId=staff-123");
        response3.StatusCode.Should().Be(HttpStatusCode.OK);
        var result3 = await response3.Content.ReadFromJsonAsync<List<RfqResponse>>();
        result3.Should().HaveCount(1);
        result3![0].AssignedStaffUserId.Should().Be("staff-123");
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
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var noteResponse = await response.Content.ReadFromJsonAsync<InternalNoteResponse>();
        noteResponse.Should().NotBeNull();
        noteResponse!.Content.Should().Be("Customer requested urgent delivery");
        noteResponse.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify database persistence
        var savedNote = await DbContext.InternalNotes
            .FirstOrDefaultAsync(n => n.RfqId == rfq.Id);

        savedNote.Should().NotBeNull();
        savedNote!.Content.Should().Be("Customer requested urgent delivery");
    }
}
