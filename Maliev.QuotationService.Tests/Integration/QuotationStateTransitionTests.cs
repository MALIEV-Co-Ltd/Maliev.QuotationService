using System.Net;
using System.Net.Http.Json;
using Xunit;
using Maliev.QuotationService.Api.DTOs.Requests;
using Maliev.QuotationService.Api.DTOs.Responses;
using Maliev.QuotationService.Domain.Entities;
using Maliev.QuotationService.Domain.Enums;
using Maliev.QuotationService.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Maliev.QuotationService.Tests.Integration;

public class QuotationStateTransitionTests : BaseIntegrationTest
{
    public QuotationStateTransitionTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task UpdateStatus_ValidTransition_Returns200()
    {
        // Arrange
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "status-test@example.com",
            Name = "Status Test Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Status = QuotationStatus.Draft,
            ValidityPeriodStart = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidityPeriodEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Customers.Add(customer);
        DbContext.Quotations.Add(quotation);
        await DbContext.SaveChangesAsync();

        using var authenticatedClient = CreateAuthenticatedClient();

        var statusRequest = new
        {
            Status = QuotationStatus.PendingApproval
        };

        // Act
        var response = await authenticatedClient.PatchAsync(
            $"/quotation/v1/quotations/{quotation.Id}/status",
            JsonContent.Create(statusRequest));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var quotationResponse = await response.Content.ReadFromJsonAsync<QuotationResponse>();
        Assert.NotNull(quotationResponse);
        Assert.Equal(QuotationStatus.PendingApproval, quotationResponse.Status);

        // Verify in database (detach and reload to get fresh data)
        DbContext.Entry(quotation).State = EntityState.Detached;
        var updatedQuotation = await DbContext.Quotations.FindAsync(quotation.Id);
        Assert.Equal(QuotationStatus.PendingApproval, updatedQuotation!.Status);

        // Verify audit log
        var auditLog = await DbContext.AuditLogEntries
            .FirstOrDefaultAsync(a => a.EntityId == quotation.Id && a.ActionType == AuditActionType.Update);
        Assert.NotNull(auditLog);
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_Returns400()
    {
        // Arrange
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "invalid-transition@example.com",
            Name = "Invalid Transition Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Status = QuotationStatus.Draft,
            ValidityPeriodStart = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidityPeriodEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Customers.Add(customer);
        DbContext.Quotations.Add(quotation);
        await DbContext.SaveChangesAsync();

        using var authenticatedClient = CreateAuthenticatedClient();

        var statusRequest = new
        {
            Status = QuotationStatus.Accepted  // Invalid: Can't go directly from Draft to Accepted
        };

        // Act
        var response = await authenticatedClient.PatchAsync(
            $"/quotation/v1/quotations/{quotation.Id}/status",
            JsonContent.Create(statusRequest));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Verify status unchanged in database
        DbContext.Entry(quotation).State = EntityState.Detached;
        var unchangedQuotation = await DbContext.Quotations.FindAsync(quotation.Id);
        Assert.Equal(QuotationStatus.Draft, unchangedQuotation!.Status);
    }

    [Fact]
    public async Task ApproveQuotation_ManagerRole_Returns200()
    {
        // Arrange
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "approve-test@example.com",
            Name = "Approve Test Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Status = QuotationStatus.PendingApproval,
            ValidityPeriodStart = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidityPeriodEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Customers.Add(customer);
        DbContext.Quotations.Add(quotation);
        await DbContext.SaveChangesAsync();

        // Create authenticated client with Manager role
        using var managerClient = CreateAuthenticatedClient("manager-user", new[] { "Manager" });

        // Act
        var response = await managerClient.PostAsync(
            $"/quotation/v1/quotations/{quotation.Id}/approve",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var quotationResponse = await response.Content.ReadFromJsonAsync<QuotationResponse>();
        Assert.NotNull(quotationResponse);
        Assert.Equal(QuotationStatus.Approved, quotationResponse.Status);

        // Verify in database (detach and reload to get fresh data)
        DbContext.Entry(quotation).State = EntityState.Detached;
        var approvedQuotation = await DbContext.Quotations.FindAsync(quotation.Id);
        Assert.Equal(QuotationStatus.Approved, approvedQuotation!.Status);

        // Verify audit log contains approval information
        var auditLog = await DbContext.AuditLogEntries
            .FirstOrDefaultAsync(a => a.EntityId == quotation.Id && a.UserId == "manager-user");
        Assert.NotNull(auditLog);
    }

    [Fact]
    public async Task ApproveQuotation_EmployeeRole_Returns403()
    {
        // Arrange
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "forbidden-approve@example.com",
            Name = "Forbidden Approve Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Status = QuotationStatus.PendingApproval,
            ValidityPeriodStart = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidityPeriodEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Customers.Add(customer);
        DbContext.Quotations.Add(quotation);
        await DbContext.SaveChangesAsync();

        // Create authenticated client with Employee role (not Manager)
        using var employeeClient = CreateAuthenticatedClient("employee-user", new[] { "Employee" });

        // Act
        var response = await employeeClient.PostAsync(
            $"/quotation/v1/quotations/{quotation.Id}/approve",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Verify status unchanged in database
        DbContext.Entry(quotation).State = EntityState.Detached;
        var unchangedQuotation = await DbContext.Quotations.FindAsync(quotation.Id);
        Assert.Equal(QuotationStatus.PendingApproval, unchangedQuotation!.Status);
    }

    [Fact]
    public async Task ApproveQuotation_WrongStatus_Returns400()
    {
        // Arrange
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "wrong-status@example.com",
            Name = "Wrong Status Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Status = QuotationStatus.Draft,  // Not PendingApproval
            ValidityPeriodStart = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidityPeriodEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Customers.Add(customer);
        DbContext.Quotations.Add(quotation);
        await DbContext.SaveChangesAsync();

        using var managerClient = CreateAuthenticatedClient("manager-user", new[] { "Manager" });

        // Act
        var response = await managerClient.PostAsync(
            $"/quotation/v1/quotations/{quotation.Id}/approve",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Verify status unchanged
        DbContext.Entry(quotation).State = EntityState.Detached;
        var unchangedQuotation = await DbContext.Quotations.FindAsync(quotation.Id);
        Assert.Equal(QuotationStatus.Draft, unchangedQuotation!.Status);
    }

    [Fact]
    public async Task AddNoteToQuotation_ValidNote_Returns201()
    {
        // Arrange
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Email = "note-test@example.com",
            Name = "Note Test Customer",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Status = QuotationStatus.Draft,
            ValidityPeriodStart = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidityPeriodEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Customers.Add(customer);
        DbContext.Quotations.Add(quotation);
        await DbContext.SaveChangesAsync();

        using var authenticatedClient = CreateAuthenticatedClient();

        var noteRequest = new AddInternalNoteRequest
        {
            Content = "This is a test note for the quotation"
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync(
            $"/quotation/v1/quotations/{quotation.Id}/notes",
            noteRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var noteResponse = await response.Content.ReadFromJsonAsync<InternalNoteResponse>();
        Assert.NotNull(noteResponse);
        Assert.Equal("This is a test note for the quotation", noteResponse.Content);

        // Verify in database
        var note = await DbContext.InternalNotes
            .FirstOrDefaultAsync(n => n.QuotationId == quotation.Id);
        Assert.NotNull(note);
        Assert.Equal("This is a test note for the quotation", note.Content);

        // Verify audit log
        var auditLog = await DbContext.AuditLogEntries
            .FirstOrDefaultAsync(a => a.EntityId == quotation.Id);
        Assert.NotNull(auditLog);
    }
}
