# Tasks: Unified Quotation and RFQ Management Service

**Input**: Design documents from `/specs/001-quotation-rfq-service/`
**Prerequisites**: plan.md (✅), spec.md (✅), research.md (✅), data-model.md (✅), contracts/ (✅)

**Tests**: Tests are NOT explicitly requested in specification. Following Test-First Development (Constitution Principle III), ALL tests will be generated in Phase 2 (Foundational) before implementation begins.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Project Structure**: `Maliev.QuotationService.Api/`, `Maliev.QuotationService.Data/`, `Maliev.QuotationService.Tests/`
- All paths relative to repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create solution file Maliev.QuotationService.sln at repository root
- [X] T002 Create Maliev.QuotationService.Api project with ASP.NET Core 10.0 WebAPI template
- [X] T003 Create Maliev.QuotationService.Data class library project targeting net10.0
- [X] T004 Create Maliev.QuotationService.Tests xUnit test project targeting net10.0
- [X] T005 [P] Add nuget.config at repository root with GitHub Packages source configuration
- [X] T006 [P] Add .gitignore with .NET template (bin/, obj/, .vs/, .vscode/, etc.)
- [X] T007 [P] Add .dockerignore excluding build artifacts, specs/, tests/, .github/, IDE files
- [X] T008 Add NuGet package references to Maliev.QuotationService.Api project: Maliev.Aspire.ServiceDefaults 1.0.*, ASP.NET Core 10.0 packages, Serilog.AspNetCore 9.0.0, FluentValidation.DependencyInjectionExtensions 12.1.0, Microsoft.Extensions.Http.Resilience 10.0.0, Scalar.AspNetCore 2.11.0, Microsoft.AspNetCore.OpenApi 10.0.0, Microsoft.AspNetCore.Authentication.JwtBearer 10.0.0, Asp.Versioning.Http 8.1.0, AspNetCore.HealthChecks.UI.Client 9.0.0, MassTransit.RabbitMQ 8.5.5, StackExchange.Redis 2.10.1, Microsoft.Extensions.Caching.StackExchangeRedis 10.0.0, Prometheus.AspNetCore 8.2.1
- [X] T009 Add NuGet package references to Maliev.QuotationService.Data project: Microsoft.EntityFrameworkCore 10.0.0, Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0, Microsoft.EntityFrameworkCore.Design 10.0.0
- [X] T010 Add NuGet package references to Maliev.QuotationService.Tests project: xUnit 2.9.2, xunit.runner.visualstudio 3.0.0, Microsoft.AspNetCore.Mvc.Testing 10.0.0, FluentAssertions 7.0.0, Testcontainers 4.0.0, Testcontainers.PostgreSql 4.0.0, Testcontainers.RabbitMq 4.0.0, Testcontainers.Redis 4.0.0
- [X] T011 Create appsettings.json in Maliev.QuotationService.Api with ConnectionStrings, ExternalServices, Jwt, RabbitMQ, Redis placeholder configuration
- [X] T012 Create appsettings.Development.json in Maliev.QuotationService.Api with Redis.Enabled=false, RabbitMQ.Enabled=false for standalone development
- [X] T013 Create Properties/launchSettings.json in Maliev.QuotationService.Api with http and https profiles (ports 5272/7272), launchUrl pointing to quotation/scalar/v1
- [X] T014 Create Dockerfile in Maliev.QuotationService.Api/ following MALIEV standard multi-stage build pattern with BuildKit secrets for NuGet authentication
- [X] T015 [P] Add project references: Api references Data, Tests references Api and Data

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Database & Entity Framework Setup

- [X] T016 Create QuotationDbContext.cs in Maliev.QuotationService.Data/ with DbSet properties for all 11 entities
- [X] T017 Create Entities directory in Maliev.QuotationService.Data/ with empty class files for all 11 entities: Customer.cs, Rfq.cs, Quotation.cs, QuotationVersion.cs, QuotationLineItem.cs, DiscountStructure.cs, InternalNote.cs, FileReference.cs, MaterialReference.cs, AuditLogEntry.cs, StaffRole.cs
- [X] T018 Create Configurations directory in Maliev.QuotationService.Data/ with empty configuration class files for all 11 entities
- [X] T019 Implement Customer entity in Maliev.QuotationService.Data/Entities/Customer.cs with Id, Email, PhoneNumber, Name, ContactInfo (JSONB), MergedFromIds, MergeHistory (JSONB), CreatedAt, UpdatedAt, IsDeleted, DeletedAt
- [X] T020 Implement CustomerConfiguration in Maliev.QuotationService.Data/Configurations/CustomerConfiguration.cs with table mapping, indexes (Email UNIQUE, PhoneNumber, Name full-text), query filter for soft delete
- [X] T021 [P] Implement Rfq entity in Maliev.QuotationService.Data/Entities/Rfq.cs with Id, CustomerId, ChannelSource (enum), Status (enum), RequestDetails (JSONB), AssignedStaffUserId, ConvertedToQuotationId, CreatedAt, UpdatedAt, IsDeleted, DeletedAt
- [X] T022 [P] Implement RfqConfiguration in Maliev.QuotationService.Data/Configurations/RfqConfiguration.cs with table mapping, foreign keys (Customer, Quotation), composite index on (ChannelSource, Status, CreatedAt), query filter
- [X] T023 [P] Implement Quotation entity in Maliev.QuotationService.Data/Entities/Quotation.cs with Id, CustomerId, SourceRfqId, CurrentVersionId, Status (enum), ValidityPeriodStart, ValidityPeriodEnd, CreatedAt, UpdatedAt, RowVersion (optimistic concurrency), IsDeleted, DeletedAt
- [X] T024 [P] Implement QuotationConfiguration in Maliev.QuotationService.Data/Configurations/QuotationConfiguration.cs with table mapping, foreign keys, composite index on (Status, ValidityPeriodEnd, CreatedAt), RowVersion as concurrency token
- [X] T025 [P] Implement QuotationVersion entity in Maliev.QuotationService.Data/Entities/QuotationVersion.cs with Id, QuotationId, VersionNumber, CreatedByUserId, CreatedAt, ChangeSummary, TotalPrice, CurrencyCode, DeliveryExpectations, SpecialTerms
- [X] T026 [P] Implement QuotationVersionConfiguration in Maliev.QuotationService.Data/Configurations/QuotationVersionConfiguration.cs with table mapping, foreign key to Quotation, unique index on (QuotationId, VersionNumber)
- [X] T027 [P] Implement QuotationLineItem entity in Maliev.QuotationService.Data/Entities/QuotationLineItem.cs with Id, VersionId, LineNumber, MaterialServiceId, MaterialName, MaterialProperties (JSONB), ManufacturingProcess, Quantity, UnitOfMeasure, UnitPrice, LineTotal, Notes
- [X] T028 [P] Implement QuotationLineItemConfiguration in Maliev.QuotationService.Data/Configurations/QuotationLineItemConfiguration.cs with table mapping, foreign key to QuotationVersion, unique index on (VersionId, LineNumber)
- [X] T029 [P] Implement DiscountStructure entity in Maliev.QuotationService.Data/Entities/DiscountStructure.cs with Id, QuotationVersionId, DiscountType (enum), DiscountValue, Conditions, AuthorizationReason, AuthorizedByUserId, AppliedAt
- [X] T030 [P] Implement DiscountStructureConfiguration in Maliev.QuotationService.Data/Configurations/DiscountStructureConfiguration.cs with table mapping, foreign key to QuotationVersion
- [X] T031 [P] Implement InternalNote entity in Maliev.QuotationService.Data/Entities/InternalNote.cs with Id, RfqId, QuotationId, AuthorUserId, Content, CreatedAt, IsDeleted, DeletedAt
- [X] T032 [P] Implement InternalNoteConfiguration in Maliev.QuotationService.Data/Configurations/InternalNoteConfiguration.cs with table mapping, CHECK constraint (exactly one of RfqId or QuotationId), query filter
- [X] T033 [P] Implement FileReference entity in Maliev.QuotationService.Data/Entities/FileReference.cs with Id, RfqId, QuotationId, UploadServiceFileId, FileName, FileType, UploadedAt, UploadedByUserId
- [X] T034 [P] Implement FileReferenceConfiguration in Maliev.QuotationService.Data/Configurations/FileReferenceConfiguration.cs with table mapping, UNIQUE index on UploadServiceFileId, CHECK constraint
- [X] T035 [P] Implement MaterialReference entity in Maliev.QuotationService.Data/Entities/MaterialReference.cs with Id, MaterialServiceId, MaterialName, MechanicalProperties (JSONB), SupportedProcesses (array), AvailabilityStatus (enum), CachedAt, ExpiresAt
- [X] T036 [P] Implement MaterialReferenceConfiguration in Maliev.QuotationService.Data/Configurations/MaterialReferenceConfiguration.cs with table mapping, UNIQUE index on MaterialServiceId, index on ExpiresAt
- [X] T037 [P] Implement AuditLogEntry entity in Maliev.QuotationService.Data/Entities/AuditLogEntry.cs with Id, EntityType (enum), EntityId, UserId, ActionType (enum), Timestamp, ChangedFields (JSONB), IpAddress, UserAgent
- [X] T038 [P] Implement AuditLogEntryConfiguration in Maliev.QuotationService.Data/Configurations/AuditLogEntryConfiguration.cs with table mapping, composite index on (EntityType, EntityId, Timestamp)
- [X] T039 [P] Implement StaffRole entity in Maliev.QuotationService.Data/Entities/StaffRole.cs with Id, RoleName, Permissions (array), Description, CreatedAt, UpdatedAt
- [X] T040 [P] Implement StaffRoleConfiguration in Maliev.QuotationService.Data/Configurations/StaffRoleConfiguration.cs with table mapping, UNIQUE index on RoleName
- [X] T041 Implement all enums in Maliev.QuotationService.Data/Enums/: RfqChannel, RfqStatus, QuotationStatus, DiscountType, MaterialAvailabilityStatus, AuditEntityType, AuditActionType
- [X] T042 Apply all entity configurations to QuotationDbContext in OnModelCreating using modelBuilder.ApplyConfigurationsFromAssembly
- [X] T043 Create initial EF Core migration "InitialCreate" using dotnet ef migrations add InitialCreate
- [X] T044 Verify migration generates correct PostgreSQL schema with all tables, indexes, constraints, foreign keys, and CHECK constraints

### Configuration & Settings

- [X] T045 [P] Create Configuration/Settings/JwtSettings.cs in Maliev.QuotationService.Api/ with PublicKey, Issuer, Audience properties
- [X] T046 [P] Create Configuration/Settings/RedisSettings.cs in Maliev.QuotationService.Api/ with Enabled, ConnectionString properties
- [X] T047 [P] Create Configuration/Settings/RabbitMQSettings.cs in Maliev.QuotationService.Api/ with Enabled, Host, Port, Username, Password, VirtualHost properties
- [X] T048 [P] Create Configuration/Settings/ExternalServicesSettings.cs in Maliev.QuotationService.Api/ with nested classes for Currency, Material, Upload, PDF services (BaseUrl, TimeoutInSeconds)

### Service Extensions

- [X] T049 Create Configuration/Extensions/ServiceCollectionExtensions.cs in Maliev.QuotationService.Api/ with AddQuotationDbContext method configuring Npgsql with connection string from appsettings
- [X] T050 Implement AddRedisCache extension method in ServiceCollectionExtensions.cs with fallback to in-memory cache when Redis.Enabled=false
- [X] T051 Implement AddMassTransitWithRabbitMq extension method in ServiceCollectionExtensions.cs with fallback to in-memory transport when RabbitMQ.Enabled=false
- [X] T052 Create Configuration/Extensions/AuthenticationExtensions.cs in Maliev.QuotationService.Api/ with AddJwtAuthentication method implementing RSA public key validation (Base64 decode → PEM → DER → RSA import)
- [X] T053 Implement AddAuthorizationPolicies extension method in AuthenticationExtensions.cs with policies: Customer, Employee, Manager, Admin, EmployeeOrHigher
- [X] T054 Create Configuration/Extensions/RateLimitingExtensions.cs in Maliev.QuotationService.Api/ with AddRateLimiting method configuring global (100 req/min) and batch (10 req/min) policies using PartitionedRateLimiter
- [X] T055 Create Configuration/Extensions/ObservabilityExtensions.cs in Maliev.QuotationService.Api/ with AddObservability method configuring Serilog JSON logging to stdout, Prometheus metrics with service_name/version/region/environment tags

### Middleware

- [X] T056 [P] Create Middleware/ExceptionHandlingMiddleware.cs in Maliev.QuotationService.Api/ implementing global exception handler with RFC 7807 Problem Details format for 400/401/403/404/409/503 responses
- [X] T057 [P] Create Middleware/RequestLoggingMiddleware.cs in Maliev.QuotationService.Api/ implementing structured logging with correlation ID, user ID, request path, duration

### External Service Clients (Interfaces)

- [X] T058 [P] Create ExternalClients/Interfaces/ICurrencyServiceClient.cs in Maliev.QuotationService.Api/ with GetConversionRate and GetAvailableCurrencies methods
- [X] T059 [P] Create ExternalClients/Interfaces/IMaterialServiceClient.cs in Maliev.QuotationService.Api/ with GetMaterialById, GetSupportedProcesses methods
- [X] T060 [P] Create ExternalClients/Interfaces/IUploadServiceClient.cs in Maliev.QuotationService.Api/ with ValidateFileReference method
- [X] T061 [P] Create ExternalClients/Interfaces/IPdfServiceClient.cs in Maliev.QuotationService.Api/ with GeneratePdf method accepting structured quotation payload

### External Service Clients (Implementations)

- [X] T062 [P] Implement CurrencyServiceClient.cs in Maliev.QuotationService.Api/ExternalClients/ using typed HttpClient with AddStandardResilienceHandler (retry + circuit breaker)
- [X] T063 [P] Implement MaterialServiceClient.cs in Maliev.QuotationService.Api/ExternalClients/ using typed HttpClient with AddStandardResilienceHandler
- [X] T064 [P] Implement UploadServiceClient.cs in Maliev.QuotationService.Api/ExternalClients/ using typed HttpClient with AddStandardResilienceHandler
- [X] T065 [P] Implement PdfServiceClient.cs in Maliev.QuotationService.Api/ExternalClients/ using typed HttpClient with AddStandardResilienceHandler
- [X] T066 Register all external service clients in ServiceCollectionExtensions with base URL and timeout from ExternalServicesSettings

### Test Infrastructure (Testcontainers)

- [X] T067 Create Fixtures/IntegrationTestWebAppFactory.cs in Maliev.QuotationService.Tests/ implementing WebApplicationFactory<Program> and IAsyncLifetime
- [X] T068 Implement IntegrationTestWebAppFactory constructor initializing PostgreSqlContainer (postgres:18), RabbitMqContainer (rabbitmq:3-management), RedisContainer (redis:7.0)
- [X] T069 Implement IntegrationTestWebAppFactory.InitializeAsync starting all 3 containers in parallel with Task.WhenAll
- [X] T070 Implement IntegrationTestWebAppFactory.DisposeAsync stopping all 3 containers in parallel
- [X] T071 Implement IntegrationTestWebAppFactory.ConfigureWebHost overriding DbContext connection string with Testcontainers PostgreSQL connection string, applying migrations with dbContext.Database.Migrate(), overriding RabbitMQ and Redis settings with Testcontainers ports
- [X] T072 Create Fixtures/BaseIntegrationTest.cs in Maliev.QuotationService.Tests/ implementing IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime with HttpClient, ServiceScope, DbContext properties
- [X] T073 Implement BaseIntegrationTest.DisposeAsync with ResetDatabaseAsync method using manual DELETEs to clear all tables (no TRUNCATE due to foreign keys)

### Program.cs Setup

- [X] T074 Implement Program.cs in Maliev.QuotationService.Api/ with builder.AddServiceDefaults(), configuration loading from /mnt/secrets if directory exists, service registrations (DbContext, Redis, MassTransit, authentication, authorization, rate limiting, observability, FluentValidation, external clients), middleware pipeline in correct order (UseHttpMetrics → UseRateLimiter → UseAuthentication → UseAuthorization), explicit route prefixes (NO UsePathBase), Scalar configuration with MapOpenApi("/quotation/openapi/{documentName}.json") and MapScalarApiReference("/quotation/scalar/v1"), health check endpoints (/quotation/liveness, /quotation/readiness), metrics endpoint (/quotation/metrics), app.MapDefaultEndpoints()
- [X] T075 Add redirect routes in Program.cs: MapGet("/", () => Results.Redirect("/quotation/scalar/v1")) and MapGet("/quotation", () => Results.Redirect("/quotation/scalar/v1"))

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Multi-Channel RFQ Intake and Tracking (Priority: P1) 🎯 MVP

**Goal**: Enable MALIEV staff to capture and track all customer requests for quotations from diverse channels, update details, add notes, assign to staff, and convert to quotations

**Independent Test**: Submit RFQs through multiple channels (website, LINE, email), verify staff can view, update, add notes, assign, and track each RFQ independently

### Tests for User Story 1 ⚠️ (Test-First Development - MUST FAIL before implementation)

- [X] T076 [P] [US1] Write unit test RfqServiceTests.CreateAsync_ValidRequest_CreatesRfq in Maliev.QuotationService.Tests/Unit/Services/RfqServiceTests.cs
- [X] T077 [P] [US1] Write unit test RfqServiceTests.UpdateAsync_ValidRfq_UpdatesDetails in Maliev.QuotationService.Tests/Unit/Services/RfqServiceTests.cs
- [X] T078 [P] [US1] Write unit test RfqServiceTests.AssignAsync_ValidStaffUser_AssignsRfq in Maliev.QuotationService.Tests/Unit/Services/RfqServiceTests.cs
- [X] T079 [P] [US1] Write integration test RfqEndpointsTests.CreateRfq_ValidRequest_ReturnsCreated in Maliev.QuotationService.Tests/Integration/RfqEndpointsTests.cs using IntegrationTestWebAppFactory
- [X] T080 [P] [US1] Write integration test RfqEndpointsTests.GetRfqs_WithFilters_ReturnsFilteredResults in Maliev.QuotationService.Tests/Integration/RfqEndpointsTests.cs
- [X] T081 [P] [US1] Write integration test RfqEndpointsTests.AddNote_ValidNote_AddsToRfq in Maliev.QuotationService.Tests/Integration/RfqEndpointsTests.cs
- [X] T082 [P] [US1] Write integration test AuditTrailTests.CreateRfq_CreatesAuditLogEntry in Maliev.QuotationService.Tests/Integration/AuditTrailTests.cs

### DTOs for User Story 1

- [X] T083 [P] [US1] Create DTOs/Requests/CreateRfqRequest.cs in Maliev.QuotationService.Api/ with CustomerEmail, CustomerName, CustomerPhoneNumber, ChannelSource, RequestDetails, UploadServiceFileIds (List<Guid>, nullable)
- [X] T084 [P] [US1] Create DTOs/Requests/UpdateRfqRequest.cs in Maliev.QuotationService.Api/ with RequestDetails, AssignedStaffUserId
- [X] T085 [P] [US1] Create DTOs/Requests/UpdateRfqStatusRequest.cs in Maliev.QuotationService.Api/ with Status
- [X] T086 [P] [US1] Create DTOs/Requests/AddInternalNoteRequest.cs in Maliev.QuotationService.Api/ with Content
- [X] T087 [P] [US1] Create DTOs/Requests/AssignRfqRequest.cs in Maliev.QuotationService.Api/ with AssignedStaffUserId
- [X] T088 [P] [US1] Create DTOs/Responses/RfqResponse.cs in Maliev.QuotationService.Api/ with Id, Customer (nested DTO), ChannelSource, Status, RequestDetails, AssignedStaffUserId, CreatedAt, UpdatedAt
- [X] T089 [P] [US1] Create DTOs/Responses/InternalNoteResponse.cs in Maliev.QuotationService.Api/ with Id, AuthorUserId, Content, CreatedAt

### Validators for User Story 1

- [X] T090 [P] [US1] Create Validators/CreateRfqRequestValidator.cs in Maliev.QuotationService.Api/ using FluentValidation with rules: Email required and valid format, Name required (1-200 chars), ChannelSource valid enum
- [X] T091 [P] [US1] Create Validators/UpdateRfqRequestValidator.cs in Maliev.QuotationService.Api/ with validation for optional fields
- [X] T092 [P] [US1] Create Validators/AddInternalNoteRequestValidator.cs in Maliev.QuotationService.Api/ with Content required and non-empty

### Services for User Story 1

- [X] T093 Create Services/Interfaces/IRfqService.cs in Maliev.QuotationService.Api/ with CreateAsync, GetByIdAsync, GetAllAsync (with filtering), UpdateAsync, UpdateStatusAsync, AddNoteAsync, AssignAsync, ConvertToQuotationAsync methods
- [X] T094 Implement RfqService.cs in Maliev.QuotationService.Api/Services/ with constructor injecting QuotationDbContext, ILogger
- [X] T095 [US1] Implement RfqService.CreateAsync in Maliev.QuotationService.Api/Services/RfqService.cs with customer lookup/creation, RFQ creation, audit log entry creation, SaveChangesAsync
- [X] T096 [US1] Implement RfqService.GetByIdAsync in Maliev.QuotationService.Api/Services/RfqService.cs with Include for Customer, FileReferences, InternalNotes
- [X] T097 [US1] Implement RfqService.GetAllAsync in Maliev.QuotationService.Api/Services/RfqService.cs with filtering by ChannelSource, Status, CustomerId, AssignedStaffUserId, date range, pagination
- [X] T098 [US1] Implement RfqService.UpdateAsync in Maliev.QuotationService.Api/Services/RfqService.cs with audit log entry creation
- [X] T099 [US1] Implement RfqService.UpdateStatusAsync in Maliev.QuotationService.Api/Services/RfqService.cs with status transition validation, audit log entry
- [X] T100 [US1] Implement RfqService.AddNoteAsync in Maliev.QuotationService.Api/Services/RfqService.cs creating InternalNote entity, audit log entry
- [X] T101 [US1] Implement RfqService.AssignAsync in Maliev.QuotationService.Api/Services/RfqService.cs with staff user validation (exists in identity system), audit log entry

### Controllers for User Story 1

- [X] T102 Create Controllers/v1/RfqController.cs in Maliev.QuotationService.Api/ with [ApiController], [Route("quotation/v{version:apiVersion}/rfqs")], [Authorize(Policy = "EmployeeOrHigher")] attributes
- [X] T103 [US1] Implement POST /rfqs endpoint in RfqController.cs calling RfqService.CreateAsync, returning CreatedAtAction with 201 status
- [X] T104 [US1] Implement GET /rfqs endpoint in RfqController.cs calling RfqService.GetAllAsync with query parameters (channel, status, customerId, assignedStaffUserId, fromDate, toDate, page, pageSize), returning 200 with pagination metadata
- [X] T105 [US1] Implement GET /rfqs/{id} endpoint in RfqController.cs calling RfqService.GetByIdAsync, returning 200 or 404
- [X] T106 [US1] Implement PUT /rfqs/{id} endpoint in RfqController.cs calling RfqService.UpdateAsync, returning 200 or 404
- [X] T107 [US1] Implement PATCH /rfqs/{id}/status endpoint in RfqController.cs calling RfqService.UpdateStatusAsync, returning 200 or 404
- [X] T108 [US1] Implement POST /rfqs/{id}/notes endpoint in RfqController.cs calling RfqService.AddNoteAsync, returning 201
- [X] T109 [US1] Implement PATCH /rfqs/{id}/assign endpoint in RfqController.cs with [Authorize(Policy = "Employee")] calling RfqService.AssignAsync, returning 200 or 404

### Integration & Validation for User Story 1

- [X] T110 [US1] Add DTO mapping logic (manual or AutoMapper) in RfqService.cs for entity → DTO conversions
- [X] T111 [US1] Register IRfqService → RfqService in ServiceCollectionExtensions.cs with scoped lifetime
- [X] T112 [US1] Register all User Story 1 validators with AddValidatorsFromAssemblyContaining in Program.cs
- [X] T113 [US1] Run all User Story 1 tests and verify they pass (previously failed before implementation)
- [X] T114 [US1] Manually test RFQ creation via Scalar UI at /quotation/scalar/v1, verify database persistence, audit log entries

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently. Staff can create, view, update, filter, add notes, and assign RFQs from any channel.

---

## Phase 4: User Story 2 - Quotation Creation and Versioning (Priority: P1)

**Goal**: Enable MALIEV staff to create quotations from RFQs or independently, add line items with materials and pricing, revise quotations while preserving version history

**Independent Test**: Create quotation from RFQ, add line items with materials and pricing, save multiple versions, verify history is preserved

### Tests for User Story 2 ⚠️ (Test-First Development - MUST FAIL before implementation)

- [X] T115 [P] [US2] Write unit test QuotationServiceTests.CreateAsync_ValidRequest_CreatesQuotation in Maliev.QuotationService.Tests/Unit/Services/QuotationServiceTests.cs
- [X] T116 [P] [US2] Write unit test QuotationServiceTests.CreateAsync_FromRfq_LinksToRfq in Maliev.QuotationService.Tests/Unit/Services/QuotationServiceTests.cs
- [X] T117 [P] [US2] Write unit test QuotationServiceTests.UpdateAsync_CreatesNewVersion in Maliev.QuotationService.Tests/Unit/Services/QuotationServiceTests.cs
- [X] T118 [P] [US2] Write integration test QuotationEndpointsTests.CreateQuotation_ValidRequest_ReturnsCreated in Maliev.QuotationService.Tests/Integration/QuotationEndpointsTests.cs
- [X] T119 [P] [US2] Write integration test QuotationEndpointsTests.GetVersions_MultipleVersions_ReturnsAllVersions in Maliev.QuotationService.Tests/Integration/QuotationEndpointsTests.cs
- [X] T120 [P] [US2] Write contract test MaterialServiceContractTests.GetMaterialById_ValidId_ReturnsData in Maliev.QuotationService.Tests/Contract/MaterialServiceContractTests.cs

### DTOs for User Story 2

- [X] T121 [P] [US2] Create DTOs/Requests/CreateQuotationRequest.cs in Maliev.QuotationService.Api/ with CustomerId, SourceRfqId (nullable), ValidityPeriodStart, ValidityPeriodEnd, LineItems (array), DiscountStructure (nested), DeliveryExpectations
- [X] T122 [P] [US2] Create DTOs/Requests/UpdateQuotationRequest.cs in Maliev.QuotationService.Api/ with LineItems, DiscountStructure, DeliveryExpectations, ChangeSummary
- [X] T123 [P] [US2] Create DTOs/Requests/QuotationLineItemDto.cs in Maliev.QuotationService.Api/ with MaterialServiceId, Quantity, UnitOfMeasure, UnitPrice, ManufacturingProcess, Notes
- [X] T124 [P] [US2] Create DTOs/Requests/DiscountStructureDto.cs in Maliev.QuotationService.Api/ with DiscountType, DiscountValue, Conditions, AuthorizationReason
- [X] T125 [P] [US2] Create DTOs/Responses/QuotationResponse.cs in Maliev.QuotationService.Api/ with Id, Customer (nested), SourceRfqId, CurrentVersionNumber, Status, ValidityPeriodStart, ValidityPeriodEnd, CreatedAt, UpdatedAt
- [X] T126 [P] [US2] Create DTOs/Responses/QuotationVersionResponse.cs in Maliev.QuotationService.Api/ with Id, VersionNumber, LineItems, TotalPrice, CurrencyCode, DiscountStructure, DeliveryExpectations, ChangeSummary, CreatedByUserId, CreatedAt

### Validators for User Story 2

- [X] T127 [P] [US2] Create Validators/CreateQuotationRequestValidator.cs in Maliev.QuotationService.Api/ with rules: CustomerId or SourceRfqId required, ValidityPeriodEnd > ValidityPeriodStart, LineItems not empty, each LineItem has valid MaterialServiceId
- [X] T128 [P] [US2] Create Validators/UpdateQuotationRequestValidator.cs in Maliev.QuotationService.Api/ with validation for optional fields
- [X] T129 [P] [US2] Create Validators/QuotationLineItemDtoValidator.cs in Maliev.QuotationService.Api/ with Quantity > 0, UnitPrice >= 0

### Services for User Story 2

- [X] T130 Create Services/Interfaces/IQuotationService.cs in Maliev.QuotationService.Api/ with CreateAsync, GetByIdAsync, GetAllAsync (with filtering), UpdateAsync (creates new version), GetVersionsAsync, GetVersionByNumberAsync, UpdateStatusAsync, ApproveAsync methods
- [X] T131 Implement QuotationService.cs in Maliev.QuotationService.Api/Services/ with constructor injecting QuotationDbContext, IMaterialServiceClient, ICurrencyServiceClient, ILogger
- [X] T132 [US2] Implement QuotationService.CreateAsync in Maliev.QuotationService.Api/Services/QuotationService.cs with customer validation, RFQ linking (if SourceRfqId provided), fetching material data from MaterialServiceClient, creating Quotation + QuotationVersion + QuotationLineItems + DiscountStructure entities, calculating TotalPrice, audit log entry
- [X] T133 [US2] Implement QuotationService.GetByIdAsync in Maliev.QuotationService.Api/Services/QuotationService.cs with Include for Customer, CurrentVersion (with LineItems, DiscountStructures)
- [X] T134 [US2] Implement QuotationService.GetAllAsync in Maliev.QuotationService.Api/Services/QuotationService.cs with filtering by Status, CustomerId, date range, validityExpiring, pagination
- [X] T135 [US2] Implement QuotationService.UpdateAsync in Maliev.QuotationService.Api/Services/QuotationService.cs creating new QuotationVersion with incremented VersionNumber, copying existing line items, applying changes, updating Quotation.CurrentVersionId, audit log entry
- [X] T136 [US2] Implement QuotationService.GetVersionsAsync in Maliev.QuotationService.Api/Services/QuotationService.cs returning all versions ordered by VersionNumber
- [X] T137 [US2] Implement QuotationService.GetVersionByNumberAsync in Maliev.QuotationService.Api/Services/QuotationService.cs with Include for LineItems, DiscountStructures

### Controllers for User Story 2

- [X] T138 Create Controllers/v1/QuotationController.cs in Maliev.QuotationService.Api/ with [ApiController], [Route("quotation/v{version:apiVersion}/quotations")], [Authorize(Policy = "EmployeeOrHigher")]
- [X] T139 [US2] Implement POST /quotations endpoint in QuotationController.cs calling QuotationService.CreateAsync, returning CreatedAtAction with 201
- [X] T140 [US2] Implement GET /quotations endpoint in QuotationController.cs calling QuotationService.GetAllAsync with query parameters (status, customerId, fromDate, toDate, validityExpiring, page, pageSize), returning 200 with pagination
- [X] T141 [US2] Implement GET /quotations/{id} endpoint in QuotationController.cs calling QuotationService.GetByIdAsync, returning 200 or 404
- [X] T142 [US2] Implement PUT /quotations/{id} endpoint in QuotationController.cs calling QuotationService.UpdateAsync (creates new version), handling optimistic concurrency conflicts with 409, returning 200 or 404
- [X] T143 [US2] Implement GET /quotations/{id}/versions endpoint in QuotationController.cs calling QuotationService.GetVersionsAsync, returning 200
- [X] T144 [US2] Implement GET /quotations/{id}/versions/{versionNumber} endpoint in QuotationController.cs calling QuotationService.GetVersionByNumberAsync, returning 200 or 404

### Integration & Validation for User Story 2

- [X] T145 [US2] Implement material data caching logic in QuotationService.cs: check MaterialReference cache, if miss or expired call MaterialServiceClient, store in cache with 1-hour TTL
- [X] T146 [US2] Add DTO mapping logic in QuotationService.cs for Quotation/QuotationVersion entities → DTOs
- [X] T147 [US2] Register IQuotationService → QuotationService in ServiceCollectionExtensions.cs with scoped lifetime
- [X] T148 [US2] Register all User Story 2 validators in Program.cs
- [X] T149 [US2] Implement RfqService.ConvertToQuotationAsync in Maliev.QuotationService.Api/Services/RfqService.cs calling QuotationService.CreateAsync with SourceRfqId, updating RFQ.ConvertedToQuotationId, updating RFQ.Status to Converted
- [X] T150 [US2] Implement POST /rfqs/{id}/convert endpoint in Controllers/v1/RfqController.cs calling RfqService.ConvertToQuotationAsync, returning 201 with quotation location
- [X] T151 [US2] Run all User Story 2 tests and verify they pass
- [X] T152 [US2] Manually test quotation creation via Scalar UI, verify version history, verify material data fetched from Material Service (or cache), verify audit logs

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently. Staff can create quotations from RFQs or standalone, add line items, revise with version history.

---

## Phase 5: User Story 3 - Quotation State Management and Lifecycle (Priority: P2)

**Goal**: Enable MALIEV staff to manage quotations through state transitions (draft, pending approval, approved, customer review, accepted, expired, cancelled) with validation

**Independent Test**: Create quotation, transition through states (draft → sent → accepted or cancelled), verify invalid transitions are prevented, all changes audited

### Tests for User Story 3 ⚠️ (Test-First Development - MUST FAIL before implementation)

- [X] T153 [P] [US3] Write unit test QuotationServiceTests.UpdateStatusAsync_ValidTransition_UpdatesStatus in Maliev.QuotationService.Tests/Unit/Services/QuotationServiceTests.cs
- [X] T154 [P] [US3] Write unit test QuotationServiceTests.UpdateStatusAsync_InvalidTransition_ThrowsException in Maliev.QuotationService.Tests/Unit/Services/QuotationServiceTests.cs
- [X] T155 [P] [US3] Write unit test QuotationServiceTests.ApproveAsync_ManagerRole_ApprovesQuotation in Maliev.QuotationService.Tests/Unit/Services/QuotationServiceTests.cs
- [X] T156 [P] [US3] Write integration test QuotationEndpointsTests.UpdateStatus_ValidTransition_Returns200 in Maliev.QuotationService.Tests/Integration/QuotationEndpointsTests.cs
- [X] T157 [P] [US3] Write integration test RoleBasedAccessTests.ApproveQuotation_NonManager_Returns403 in Maliev.QuotationService.Tests/Integration/RoleBasedAccessTests.cs

### DTOs for User Story 3

- [X] T158 [P] [US3] Create DTOs/Requests/UpdateQuotationStatusRequest.cs in Maliev.QuotationService.Api/ with Status, Reason (optional)

### State Machine Logic for User Story 3

- [X] T159 [US3] Create Services/QuotationStateMachine.cs in Maliev.QuotationService.Api/Services/ with IsValidTransition static method implementing state transition rules (Draft→PendingApproval/CustomerReview/Cancelled, PendingApproval→Approved/Draft/Cancelled, Approved→CustomerReview/Cancelled, CustomerReview→Accepted/Expired/Cancelled, terminal states)
- [X] T160 [US3] Implement QuotationService.UpdateStatusAsync in Maliev.QuotationService.Api/Services/QuotationService.cs with QuotationStateMachine.IsValidTransition validation, throwing InvalidOperationException if invalid, audit log entry with previous/new status
- [X] T161 [US3] Implement QuotationService.ApproveAsync in Maliev.QuotationService.Api/Services/QuotationService.cs validating current status is PendingApproval, updating status to Approved, audit log entry

### Controllers for User Story 3

- [X] T162 [US3] Implement PATCH /quotations/{id}/status endpoint in Controllers/v1/QuotationController.cs calling QuotationService.UpdateStatusAsync, catching InvalidOperationException returning 400, returning 200 or 404
- [X] T163 [US3] Implement POST /quotations/{id}/approve endpoint in Controllers/v1/QuotationController.cs with [Authorize(Policy = "Manager")] calling QuotationService.ApproveAsync, returning 200 or 404
- [X] T164 [US3] Implement POST /quotations/{id}/notes endpoint in Controllers/v1/QuotationController.cs calling QuotationService.AddNoteAsync (similar to RFQ notes), returning 201

### Background Job for Expiration (Future Enhancement)

- [X] T165 [US3] Add comment in QuotationService.cs noting that automatic expiration (CustomerReview → Expired when ValidityPeriodEnd < NOW) should be handled by background job (not implemented in this phase)

### Integration & Validation for User Story 3

- [X] T166 [US3] Run all User Story 3 tests and verify they pass
- [X] T167 [US3] Manually test state transitions via Scalar UI, verify invalid transitions return 400, verify approval endpoint requires Manager role (403 for non-managers)

**Checkpoint**: All quotation lifecycle states should now work correctly with validation, audit trails, and role-based access control.

---

## Phase 6: User Story 4 - External Service Integration (Priority: P2)

**Goal**: Ensure quotations include accurate currency conversions, material properties, uploaded documents, and can generate PDFs via external services with resilience

**Independent Test**: Create quotation requesting material data from Material Service, apply currency conversion from Currency Service, reference uploaded files from Upload Service, generate PDF via PDF Service

### Tests for User Story 4 ⚠️ (Test-First Development - MUST FAIL before implementation)

- [ ] T168 [P] [US4] Write contract test CurrencyServiceContractTests.GetConversionRate_ValidPair_ReturnsRate in Maliev.QuotationService.Tests/Contract/CurrencyServiceContractTests.cs
- [ ] T169 [P] [US4] Write contract test UploadServiceContractTests.ValidateFileReference_ValidId_ReturnsTrue in Maliev.QuotationService.Tests/Contract/UploadServiceContractTests.cs
- [ ] T170 [P] [US4] Write contract test PdfServiceContractTests.GeneratePdf_ValidPayload_ReturnsFileId in Maliev.QuotationService.Tests/Contract/PdfServiceContractTests.cs
- [ ] T171 [P] [US4] Write integration test ExternalServiceResilienceTests.MaterialService_Unavailable_UsesCachedData in Maliev.QuotationService.Tests/Integration/ExternalServiceResilienceTests.cs
- [ ] T172 [P] [US4] Write integration test ExternalServiceResilienceTests.MaterialService_CircuitOpen_ReturnsServiceUnavailable in Maliev.QuotationService.Tests/Integration/ExternalServiceResilienceTests.cs

### DTOs for User Story 4

- [ ] T173 [P] [US4] Create DTOs/Requests/GeneratePdfRequest.cs in Maliev.QuotationService.Api/ with QuotationId, VersionNumber (optional, defaults to current)
- [ ] T174 [P] [US4] Create DTOs/Responses/PdfResponse.cs in Maliev.QuotationService.Api/ with PdfServiceFileId, FileName, GeneratedAt

### Service Enhancements for User Story 4

- [ ] T175 [US4] Implement currency conversion logic in QuotationService.CreateAsync calling CurrencyServiceClient.GetConversionRate when line item prices are in different currencies, caching rates for 15 minutes in Redis
- [ ] T176 [US4] Implement file reference validation in RfqService.CreateAsync: iterate over CreateRfqRequest.UploadServiceFileIds (if provided), call UploadServiceClient.ValidateFileReference for each file ID, create and store FileReference entity with UploadServiceFileId, FileName, FileType, RfqId for each valid file only
- [ ] T177 [US4] Create Services/Interfaces/IPdfGenerationService.cs in Maliev.QuotationService.Api/ with GeneratePdfAsync method
- [ ] T178 [US4] Implement PdfGenerationService.cs in Maliev.QuotationService.Api/Services/ calling QuotationService.GetVersionByNumberAsync (or current version), mapping to structured PDF payload, calling PdfServiceClient.GeneratePdf
- [ ] T179 [US4] Register IPdfGenerationService → PdfGenerationService in ServiceCollectionExtensions.cs

### Controllers for User Story 4

- [ ] T180 [US4] Implement GET /quotations/{id}/pdf endpoint in Controllers/v1/QuotationController.cs with optional query parameter versionNumber, calling PdfGenerationService.GeneratePdfAsync, returning PdfResponse with 200 or 404 or 503 (if PDF Service circuit open)

### Circuit Breaker Metrics for User Story 4

- [ ] T181 [US4] Implement circuit breaker state change event handlers in all external clients (Currency, Material, Upload, PDF) emitting Prometheus metrics when circuit opens/closes (FR-044)
- [ ] T182 [US4] Add custom Prometheus metrics in ObservabilityExtensions.cs: circuit_breaker_state (gauge), external_service_call_duration (histogram), external_service_errors_total (counter)

### Integration & Validation for User Story 4

- [ ] T183 [US4] Run all User Story 4 tests and verify they pass
- [ ] T184 [US4] Manually test external service integration via Scalar UI: create quotation with material selection (verify Material Service call or cache hit), generate PDF (verify PDF Service call), simulate Material Service outage (docker stop material-service container) and verify circuit breaker opens, cached data used, 503 response after retries exhausted

**Checkpoint**: External service integration should work reliably with retry, circuit breaker, caching, and distributed tracing.

---

## Phase 7: User Story 5 - Advanced Search, Analytics, and Reporting (Priority: P3)

**Goal**: Enable MALIEV staff and analysts to search, filter, analyze RFQs and quotations for business performance insights (conversion rates, turnaround time, channel performance, abandoned RFQs, pricing strategy)

**Independent Test**: Create multiple RFQs and quotations across different channels and time periods, run queries to analyze conversion rates, average turnaround time, channel performance

### Tests for User Story 5 ⚠️ (Test-First Development - MUST FAIL before implementation)

- [ ] T185 [P] [US5] Write unit test AnalyticsServiceTests.GetConversionRates_ByChannel_ReturnsCorrectPercentages in Maliev.QuotationService.Tests/Unit/Services/AnalyticsServiceTests.cs
- [ ] T186 [P] [US5] Write unit test AnalyticsServiceTests.GetAverageTurnaroundTime_ReturnsCorrectDuration in Maliev.QuotationService.Tests/Unit/Services/AnalyticsServiceTests.cs
- [ ] T187 [P] [US5] Write integration test AnalyticsEndpointsTests.GetConversionRates_MultipleChannels_ReturnsData in Maliev.QuotationService.Tests/Integration/AnalyticsEndpointsTests.cs

### DTOs for User Story 5

- [ ] T188 [P] [US5] Create DTOs/Responses/ConversionRateResponse.cs in Maliev.QuotationService.Api/ with ChannelSource, TotalRfqs, ConvertedToQuotations, ConversionRate (percentage)
- [ ] T189 [P] [US5] Create DTOs/Responses/TurnaroundTimeResponse.cs in Maliev.QuotationService.Api/ with AverageDays, MedianDays, MinDays, MaxDays
- [ ] T190 [P] [US5] Create DTOs/Responses/AbandonedRfqResponse.cs in Maliev.QuotationService.Api/ with Id, Customer, ChannelSource, CreatedAt, DaysSinceCreation
- [ ] T191 [P] [US5] Create DTOs/Responses/PricingStrategyResponse.cs in Maliev.QuotationService.Api/ with DiscountType, AverageDiscountValue, UsageCount, TotalDiscountedAmount
- [ ] T192 [P] [US5] Create DTOs/Responses/ChannelPerformanceResponse.cs in Maliev.QuotationService.Api/ with ChannelSource, RfqCount, QuotationCount, ConversionRate, AverageTurnaroundDays

### Services for User Story 5

- [ ] T193 Create Services/Interfaces/IAnalyticsService.cs in Maliev.QuotationService.Api/ with GetConversionRatesByChannelAsync, GetAverageTurnaroundTimeAsync, GetAbandonedRfqsAsync, GetPricingStrategyAsync, GetChannelPerformanceAsync methods
- [ ] T194 Implement AnalyticsService.cs in Maliev.QuotationService.Api/Services/ with constructor injecting QuotationDbContext, ILogger
- [ ] T195 [US5] Implement AnalyticsService.GetConversionRatesByChannelAsync in Maliev.QuotationService.Api/Services/AnalyticsService.cs with GroupBy query on RFQs by ChannelSource, calculating percentage of ConvertedToQuotationId IS NOT NULL
- [ ] T196 [US5] Implement AnalyticsService.GetAverageTurnaroundTimeAsync in Maliev.QuotationService.Api/Services/AnalyticsService.cs joining RFQs and Quotations where SourceRfqId matches, calculating average/median/min/max of (Quotation.CreatedAt - RFQ.CreatedAt)
- [ ] T197 [US5] Implement AnalyticsService.GetAbandonedRfqsAsync in Maliev.QuotationService.Api/Services/AnalyticsService.cs filtering RFQs where Status NOT IN (Converted, Abandoned) AND CreatedAt < NOW() - INTERVAL '30 days', ordering by CreatedAt
- [ ] T198 [US5] Implement AnalyticsService.GetPricingStrategyAsync in Maliev.QuotationService.Api/Services/AnalyticsService.cs querying DiscountStructures grouped by DiscountType, calculating average DiscountValue, count, sum of discounted amounts
- [ ] T199 [US5] Implement AnalyticsService.GetChannelPerformanceAsync in Maliev.QuotationService.Api/Services/AnalyticsService.cs combining data from GetConversionRatesByChannelAsync and GetAverageTurnaroundTimeAsync per channel

### Controllers for User Story 5

- [ ] T200 Create Controllers/v1/AnalyticsController.cs in Maliev.QuotationService.Api/ with [ApiController], [Route("quotation/v{version:apiVersion}/analytics")], [Authorize(Policy = "EmployeeOrHigher")]
- [ ] T201 [US5] Implement GET /analytics/conversion-rates endpoint in AnalyticsController.cs with optional query parameter channelSource, applying batch rate limiter policy, calling AnalyticsService.GetConversionRatesByChannelAsync, returning 200
- [ ] T202 [US5] Implement GET /analytics/turnaround-time endpoint in AnalyticsController.cs with optional query parameters fromDate, toDate, applying batch rate limiter, calling AnalyticsService.GetAverageTurnaroundTimeAsync, returning 200
- [ ] T203 [US5] Implement GET /analytics/abandoned-rfqs endpoint in AnalyticsController.cs with optional threshold query parameter (default 30 days), applying batch rate limiter, calling AnalyticsService.GetAbandonedRfqsAsync, returning 200
- [ ] T204 [US5] Implement GET /analytics/pricing-strategy endpoint in AnalyticsController.cs applying batch rate limiter, calling AnalyticsService.GetPricingStrategyAsync, returning 200
- [ ] T205 [US5] Implement GET /analytics/channel-performance endpoint in AnalyticsController.cs with optional fromDate, toDate query parameters, applying batch rate limiter, calling AnalyticsService.GetChannelPerformanceAsync, returning 200

### Integration & Validation for User Story 5

- [ ] T206 [US5] Register IAnalyticsService → AnalyticsService in ServiceCollectionExtensions.cs
- [ ] T207 [US5] Apply batch rate limiter policy (10 req/min) to all analytics endpoints using [EnableRateLimiting("batch")] attribute
- [ ] T208 [US5] Run all User Story 5 tests and verify they pass
- [ ] T209 [US5] Manually test analytics endpoints via Scalar UI with test data (create multiple RFQs/quotations), verify conversion rates, turnaround time, abandoned RFQs, pricing strategy, channel performance calculations are correct

**Checkpoint**: All analytics and reporting endpoints should provide accurate business insights with appropriate rate limiting.

---

## Phase 8: Customer Management (Supporting User Story 1)

**Goal**: Enable customer matching suggestions for RFQs, manual customer linking across channels, customer unified view

**Independent Test**: Submit RFQ with customer info, get match suggestions, confirm match, verify unified customer view shows all RFQs/quotations

### Tests for Customer Management ⚠️

- [ ] T210 [P] Write unit test CustomerMatchingServiceTests.GetMatchSuggestionsAsync_ExactEmailMatch_ReturnsTopScore in Maliev.QuotationService.Tests/Unit/Services/CustomerMatchingServiceTests.cs
- [ ] T211 [P] Write unit test CustomerMatchingServiceTests.GetMatchSuggestionsAsync_FuzzyNameMatch_ReturnsLowerScore in Maliev.QuotationService.Tests/Unit/Services/CustomerMatchingServiceTests.cs
- [ ] T212 [P] Write integration test CustomerMatchingTests.LinkCustomers_ValidMatch_MergesRecords in Maliev.QuotationService.Tests/Integration/CustomerMatchingTests.cs

### DTOs for Customer Management

- [ ] T213 [P] Create DTOs/Requests/GetCustomerMatchesRequest.cs in Maliev.QuotationService.Api/ with Email, PhoneNumber, Name
- [ ] T214 [P] Create DTOs/Requests/LinkCustomerRequest.cs in Maliev.QuotationService.Api/ with SourceCustomerId, TargetCustomerId
- [ ] T215 [P] Create DTOs/Responses/CustomerMatchResponse.cs in Maliev.QuotationService.Api/ with CustomerId, Email, PhoneNumber, Name, MatchConfidence (percentage), MatchingFields (array)
- [ ] T216 [P] Create DTOs/Responses/CustomerResponse.cs in Maliev.QuotationService.Api/ with Id, Email, PhoneNumber, Name, RfqHistory (array), QuotationHistory (array), CreatedAt

### Services for Customer Management

- [ ] T217 Create Services/Interfaces/ICustomerMatchingService.cs in Maliev.QuotationService.Api/ with GetMatchSuggestionsAsync, LinkCustomersAsync, UnlinkCustomersAsync methods
- [ ] T218 Implement CustomerMatchingService.cs in Maliev.QuotationService.Api/Services/ with constructor injecting QuotationDbContext, IDistributedCache, ILogger
- [ ] T219 Implement CustomerMatchingService.GetMatchSuggestionsAsync with fuzzy matching algorithm: exact email (score 100), exact phone (score 90), Levenshtein distance on name (score 70 if < 3 edits), filtering matches with confidence ≥ 60%, sorting by total score descending, returning top 5 matches, caching results for 5 minutes
- [ ] T220 Implement CustomerMatchingService.LinkCustomersAsync merging SourceCustomerId into TargetCustomerId by updating all RFQs and Quotations with SourceCustomerId to TargetCustomerId, appending SourceCustomerId to TargetCustomer.MergedFromIds, recording merge in TargetCustomer.MergeHistory (JSONB), soft-deleting SourceCustomer
- [ ] T221 Implement CustomerMatchingService.UnlinkCustomersAsync reversing merge by restoring SourceCustomer, updating RFQs/Quotations back to SourceCustomerId, removing from MergedFromIds, recording unlink in MergeHistory

### Controllers for Customer Management

- [ ] T222 Create Controllers/v1/CustomerController.cs in Maliev.QuotationService.Api/ with [ApiController], [Route("quotation/v{version:apiVersion}/customers")], [Authorize(Policy = "EmployeeOrHigher")]
- [ ] T223 Implement GET /customers/{id} endpoint in CustomerController.cs with Include for RFQs, Quotations, returning unified customer view with 200 or 404
- [ ] T224 Implement POST /customers/match endpoint in CustomerController.cs with [Authorize(Policy = "Employee")] calling CustomerMatchingService.GetMatchSuggestionsAsync, returning 200
- [ ] T225 Implement POST /customers/link endpoint in CustomerController.cs with [Authorize(Policy = "Employee")] calling CustomerMatchingService.LinkCustomersAsync, returning 200 or 404
- [ ] T226 Implement POST /customers/unlink endpoint in CustomerController.cs with [Authorize(Policy = "Employee")] calling CustomerMatchingService.UnlinkCustomersAsync, returning 200 or 404

### Integration & Validation for Customer Management

- [ ] T227 Register ICustomerMatchingService → CustomerMatchingService in ServiceCollectionExtensions.cs
- [ ] T228 Run all customer management tests and verify they pass
- [ ] T229 Manually test customer matching via Scalar UI, verify fuzzy matching accuracy, verify link/unlink operations preserve data integrity

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories, final integration, documentation

- [ ] T230 [P] Implement HealthController.cs in Maliev.QuotationService.Api/Controllers/ with GET /quotation/liveness (returns 200 OK) and GET /quotation/readiness (checks DbContext connection, returns 200 or 503)
- [ ] T231 [P] Add comprehensive XML documentation comments to all public APIs in Controllers for OpenAPI/Scalar generation
- [ ] T232 [P] Implement custom Prometheus metrics in ObservabilityExtensions.cs for business operations: rfq_created_total (counter by channel), quotation_created_total (counter), quotation_status_transitions_total (counter by from_status, to_status), customer_matches_suggested_total (counter), external_service_cache_hits_total (counter by service)
- [ ] T233 [P] Add Serilog enrichers in Program.cs for ServiceName="Maliev.QuotationService", Version from Assembly, Environment from ASPNETCORE_ENVIRONMENT
- [ ] T234 [P] Implement CORS configuration in Program.cs reading allowed origins from appsettings CORS__AllowedOrigins, allowing GET/POST/PUT/PATCH/DELETE/OPTIONS methods
- [ ] T235 [P] Add API versioning configuration in Program.cs with ApiVersion(1.0) as default
- [ ] T236 [P] Create README.md in repository root with project description, link to quickstart.md, architecture overview, constitution compliance summary
- [ ] T237 Verify all entity configurations generate correct PostgreSQL schema by reviewing migration output (snake_case table names, JSONB columns, array columns, indexes, constraints)
- [ ] T238 Run full test suite (dotnet test) and verify 100% pass rate with minimum 80% code coverage on business logic
- [ ] T239 Run quickstart.md validation: verify both http and https profiles launch successfully, Scalar UI accessible, health checks return 200, metrics endpoint returns data
- [ ] T240 Verify Docker build succeeds with BuildKit secrets for NuGet authentication
- [ ] T241 Run service in Docker container with test PostgreSQL connection, verify all endpoints work, health checks pass, logs output JSON to stdout
- [ ] T242 Security audit: verify no secrets in appsettings.json or code, JWT validation working correctly, all endpoints require authentication (except health/metrics), role-based authorization enforced
- [ ] T243 [P] Code cleanup: remove unused imports, fix any remaining code style issues, ensure zero build warnings
- [ ] T244 Final integration test: Run complete user journey (create RFQ from website → assign to staff → add notes → convert to quotation → add line items → update status to customer review → approve → generate PDF → view analytics)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational phase completion
- **User Story 2 (Phase 4)**: Depends on Foundational phase completion (independent of US1, but integrates for RFQ conversion)
- **User Story 3 (Phase 5)**: Depends on Foundational + User Story 2 (requires Quotation entity and service)
- **User Story 4 (Phase 6)**: Depends on Foundational + User Story 2 (enhances quotation with external data)
- **User Story 5 (Phase 7)**: Depends on Foundational + User Stories 1 & 2 (analyzes RFQ and Quotation data)
- **Customer Management (Phase 8)**: Depends on Foundational + User Story 1 (enhances RFQ workflow)
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - Integrates with US1 for RFQ conversion but independently testable
- **User Story 3 (P2)**: Requires US2 complete (quotation lifecycle depends on quotations existing)
- **User Story 4 (P2)**: Requires US2 complete (external integration enhances quotations)
- **User Story 5 (P3)**: Requires US1 & US2 complete (analyzes both RFQs and quotations)

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Entities/DTOs before services (can run in parallel)
- Services before controllers
- Controllers before integration
- Story complete before moving to next priority

### Parallel Opportunities

**Within Setup (Phase 1)**:
- T005 (nuget.config), T006 (.gitignore), T007 (.dockerignore) can run in parallel
- T008, T009, T010 (NuGet package references) can run in parallel after project creation

**Within Foundational (Phase 2)**:
- All entity implementations (T019, T021, T023, T025, T027, T029, T031, T033, T035, T037, T039) can run in parallel
- All configuration implementations (T020, T022, T024, T026, T028, T030, T032, T034, T036, T038, T040) can run in parallel after entities
- All settings classes (T045, T046, T047, T048) can run in parallel
- All middleware (T056, T057) can run in parallel
- All external client interfaces (T058, T059, T060, T061) can run in parallel
- All external client implementations (T062, T063, T064, T065) can run in parallel

**Within Each User Story**:
- All tests for that story can run in parallel
- All DTOs for that story can run in parallel
- All validators for that story can run in parallel
- Multiple service methods can be implemented in parallel if they don't depend on each other

**Across User Stories** (if team capacity allows):
- After Foundational phase completes, User Stories 1 and 2 can start in parallel
- User Stories 3 and 4 can start in parallel after User Story 2 completes

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together:
Task: "Write unit test RfqServiceTests.CreateAsync_ValidRequest_CreatesRfq"
Task: "Write unit test RfqServiceTests.UpdateAsync_ValidRfq_UpdatesDetails"
Task: "Write unit test RfqServiceTests.AssignAsync_ValidStaffUser_AssignsRfq"
Task: "Write integration test RfqEndpointsTests.CreateRfq_ValidRequest_ReturnsCreated"
Task: "Write integration test RfqEndpointsTests.GetRfqs_WithFilters_ReturnsFilteredResults"
Task: "Write integration test RfqEndpointsTests.AddNote_ValidNote_AddsToRfq"
Task: "Write integration test AuditTrailTests.CreateRfq_CreatesAuditLogEntry"

# Launch all DTOs for User Story 1 together:
Task: "Create DTOs/Requests/CreateRfqRequest.cs"
Task: "Create DTOs/Requests/UpdateRfqRequest.cs"
Task: "Create DTOs/Requests/UpdateRfqStatusRequest.cs"
Task: "Create DTOs/Requests/AddInternalNoteRequest.cs"
Task: "Create DTOs/Requests/AssignRfqRequest.cs"
Task: "Create DTOs/Responses/RfqResponse.cs"
Task: "Create DTOs/Responses/InternalNoteResponse.cs"

# Launch all validators for User Story 1 together:
Task: "Create Validators/CreateRfqRequestValidator.cs"
Task: "Create Validators/UpdateRfqRequestValidator.cs"
Task: "Create Validators/AddInternalNoteRequestValidator.cs"
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only)

1. Complete Phase 1: Setup (T001-T015)
2. Complete Phase 2: Foundational (T016-T075) - CRITICAL
3. Complete Phase 3: User Story 1 (T076-T114)
4. **VALIDATE**: Test User Story 1 independently (RFQ intake and tracking works)
5. Complete Phase 4: User Story 2 (T115-T152)
6. **VALIDATE**: Test User Stories 1 & 2 together (RFQ → Quotation flow works)
7. Deploy/demo MVP (core quotation workflow functional)

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (RFQ tracking MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo (Full quotation creation!)
4. Add User Story 3 → Test independently → Deploy/Demo (Lifecycle management!)
5. Add User Story 4 → Test independently → Deploy/Demo (External integration!)
6. Add User Story 5 → Test independently → Deploy/Demo (Analytics!)
7. Polish → Final production-ready release

### Parallel Team Strategy

With 3 developers after Foundational phase:

1. **Team completes Setup + Foundational together** (critical path)
2. Once Foundational is done:
   - Developer A: User Story 1 (T076-T114)
   - Developer B: User Story 2 (T115-T152)
   - Developer C: Test Infrastructure enhancement + Customer Management prep
3. After US1 & US2 complete:
   - Developer A: User Story 3 (T153-T167)
   - Developer B: User Story 4 (T168-T184)
   - Developer C: User Story 5 (T185-T209)
4. Converge for Customer Management (Phase 8) and Polish (Phase 9)

---

## Task Summary

**Total Tasks**: 244
**Breakdown**:
- Phase 1 (Setup): 15 tasks
- Phase 2 (Foundational): 60 tasks
- Phase 3 (User Story 1 - RFQ Intake): 39 tasks
- Phase 4 (User Story 2 - Quotation Creation): 38 tasks
- Phase 5 (User Story 3 - State Management): 15 tasks
- Phase 6 (User Story 4 - External Integration): 17 tasks
- Phase 7 (User Story 5 - Analytics): 25 tasks
- Phase 8 (Customer Management): 20 tasks
- Phase 9 (Polish): 15 tasks

**Parallelizable Tasks**: 94 tasks marked with [P] can run concurrently
**Test Tasks**: 34 test tasks (14% of total, covering unit, integration, and contract tests)
**Independent User Stories**: 5 user stories, each independently testable
**MVP Scope**: User Stories 1 & 2 (77 implementation tasks after foundational) deliver core value

---

## Notes

- [P] tasks = different files, no dependencies - safe to parallelize
- [Story] label maps task to specific user story for traceability and independent delivery
- Each user story should be independently completable and testable (verified via "Independent Test" criteria)
- Tests are written FIRST (Test-First Development per Constitution Principle III)
- Run tests to verify they FAIL before implementing features
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Minimum 80% code coverage required for business-critical logic (Constitution Principle III)
- Zero warnings policy enforced (Constitution Principle VIII)
