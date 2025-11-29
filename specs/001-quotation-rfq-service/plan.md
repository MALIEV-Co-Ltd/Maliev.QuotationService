# Implementation Plan: Unified Quotation and RFQ Management Service

**Branch**: `001-quotation-rfq-service` | **Date**: 2025-11-28 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-quotation-rfq-service/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

The Quotation Service provides complete lifecycle management for customer quotations and requests for quotations (RFQs) within the MALIEV microservices ecosystem. This service consolidates the previously split "quotation service" and "quotation request service" into a unified domain that captures RFQs from multiple channels (website, LINE, WhatsApp, Facebook Messenger, Instagram, email, in-store), manages quotation creation and versioning, enforces state transitions, integrates with external MALIEV services (Currency, Material, Upload, PDF), and provides comprehensive audit trails and analytics. The service implements role-based access control, manual customer matching with assisted suggestions, standard observability with distributed tracing, 7-year data retention, and retry with circuit breaker patterns for external service resilience.

## Technical Context

**Language/Version**: .NET 10 (latest LTS)
**Primary Dependencies**:
- ASP.NET Core 10.0 WebAPI
- Entity Framework Core 10.0.0 with Npgsql 10.0.0
- PostgreSQL 18
- MassTransit.RabbitMQ 8.5.5
- StackExchange.Redis 2.10.1 with Microsoft.Extensions.Caching.StackExchangeRedis 10.0.0
- Microsoft.Extensions.Http.Resilience 10.0.0 (includes Polly v8 for circuit breaker/retry)
- FluentValidation.DependencyInjectionExtensions 12.1.0
- Serilog.AspNetCore 9.0.0
- Scalar.AspNetCore 2.11.0 with Microsoft.AspNetCore.OpenApi 10.0.0
- Microsoft.AspNetCore.Authentication.JwtBearer 10.0.0
- Asp.Versioning.Http 8.1.0
- AspNetCore.HealthChecks.UI.Client 9.0.0
- Prometheus.AspNetCore 8.2.1
- Maliev.Aspire.ServiceDefaults 1.0.* (NuGet package from GitHub Packages)

**Storage**: PostgreSQL 18 (database: `quotation_app_db`)
**Testing**:
- xUnit with Microsoft.AspNetCore.Mvc.Testing
- Testcontainers 4.0.0+ (Testcontainers.PostgreSql, Testcontainers.RabbitMq, Testcontainers.Redis)
- Real infrastructure for all integration tests (no in-memory substitutes)

**Target Platform**: Linux containers (Docker) on Google Cloud Platform
**Project Type**: Microservice (follows Maliev.ServiceName.Api, Maliev.ServiceName.Data, Maliev.ServiceName.Tests pattern)
**Performance Goals**:
- RFQ capture searchable in <5 seconds (SC-001)
- 95% of search queries return in <2 seconds (SC-004)
- Analytics queries complete in <10 seconds for 10k RFQs/quotations (SC-008)
- Support 50 concurrent staff users without degradation (SC-011)

**Constraints**:
- Stateless microservice design (all state in PostgreSQL)
- 7-year data retention requirement for compliance (FR-031, FR-032)
- Retry with exponential backoff (3 attempts) + circuit breaker for external services (FR-041-044)
- Role-based access control with distinct permissions (FR-029, FR-030)
- Standard observability: structured logging, metrics, distributed tracing (FR-033-036)
- Manual customer linking with assisted matching (FR-037-040)

**Scale/Scope**:
- 10,000+ RFQs/quotations in analytics datasets
- 7 years of historical data retention
- Multi-channel RFQ intake (7 channels: website, LINE, WhatsApp, Facebook, Instagram, email, in-store)
- Integration with 4 external services (Currency, Material, Upload, PDF)
- 4+ staff roles with granular permissions (Sales Staff, Manager, Analyst, Administrator)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ I. Service Autonomy (NON-NEGOTIABLE)
- **Status**: COMPLIANT
- **Evidence**: Service owns `quotation_app_db` database with complete RFQ and Quotation domain. Integrates with Currency, Material, Upload, and PDF services strictly via API contracts (FR-014 through FR-018). No direct database access to other services.

### ✅ II. Explicit Contracts
- **Status**: COMPLIANT
- **Evidence**: OpenAPI documentation via Scalar.AspNetCore 2.11.0 at `/quotation/scalar/v1`. API versioning via Asp.Versioning.Http 8.1.0. Contracts will be defined in Phase 1.

### ✅ III. Test-First Development (NON-NEGOTIABLE)
- **Status**: COMPLIANT
- **Evidence**: Tests will be defined immediately after specification approval in `/speckit.tasks` phase. Testcontainers infrastructure mandated for PostgreSQL, RabbitMQ, Redis. Minimum 80% coverage for business-critical logic required.

### ✅ IV. Real Infrastructure Testing (NON-NEGOTIABLE)
- **Status**: COMPLIANT
- **Evidence**:
  - PostgreSQL: Testcontainers.PostgreSql with postgres:18 image
  - RabbitMQ: Testcontainers.RabbitMq with rabbitmq:3-management image
  - Redis: Testcontainers.Redis with redis:7.0 image
  - No in-memory substitutes permitted per constitution
  - IntegrationTestWebAppFactory pattern will manage all three containers with IAsyncLifetime

### ✅ V. Auditability & Observability
- **Status**: COMPLIANT
- **Evidence**:
  - Structured JSON logging via Serilog.AspNetCore 9.0.0 (FR-033)
  - Audit log for all RFQ/quotation changes with user, timestamp, action type, changed fields (FR-019, FR-020)
  - Health checks at `/quotation/liveness` and `/quotation/readiness`
  - Distributed tracing for external service calls (FR-036)

### ✅ VI. Security & Compliance
- **Status**: COMPLIANT
- **Evidence**:
  - JWT authentication via Microsoft.AspNetCore.Authentication.JwtBearer 10.0.0
  - Role-based authorization with distinct policies (Customer, Employee, Manager, Admin, EmployeeOrHigher) - FR-029, FR-030
  - RSA public key validation (asymmetric JWT)
  - 7-year data retention for compliance (FR-031)

### ✅ VII. Secrets Management & Configuration Security (NON-NEGOTIABLE)
- **Status**: COMPLIANT
- **Evidence**:
  - All secrets injected via Google Secret Manager from `/mnt/secrets`
  - Configuration loaded via `AddKeyPerFile(directoryPath: secretsPath, optional: true)`
  - No secrets in source code
  - Docker BuildKit secrets for NuGet authentication (`--mount=type=secret,id=nuget_username`)

### ✅ VIII. Zero Warnings Policy (NON-NEGOTIABLE)
- **Status**: COMPLIANT
- **Evidence**: .NET 10 project with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` enforced in CI/CD pipeline.

### ✅ IX. Clean Project Artifacts (NON-NEGOTIABLE)
- **Status**: COMPLIANT
- **Evidence**:
  - `.gitignore` excludes bin/, obj/, .vs/, etc.
  - `.dockerignore` excludes build artifacts, specs/, tests/, .github/, IDE files
  - Test projects excluded from Docker production images

### ✅ X. Docker Best Practices (NON-NEGOTIABLE)
- **Status**: COMPLIANT
- **Evidence**:
  - Multi-stage build with `mcr.microsoft.com/dotnet/sdk:10.0` (build) and `mcr.microsoft.com/dotnet/aspnet:10.0` (runtime)
  - Built-in `app` user from Microsoft ASP.NET runtime image (no custom user creation)
  - `chown -R app:app /app` BEFORE `USER app` directive
  - Health check validates `/quotation/liveness` endpoint
  - Single port 8080 exposed
  - BuildKit secrets for NuGet credentials (no ARG for secrets)

### ✅ XI. Simplicity & Maintainability
- **Status**: COMPLIANT
- **Evidence**:
  - Clean Architecture: Controllers → Services → Data
  - Stateless design (all state in PostgreSQL)
  - YAGNI applied: AutoMapper optional, manual mapping preferred for simplicity
  - Shared Maliev.Aspire.ServiceDefaults library versioned via NuGet

### ✅ XII. Business Metrics & Analytics (NON-NEGOTIABLE)
- **Status**: COMPLIANT
- **Evidence**:
  - Business metrics: RFQ intake rate, quotation creation rate, conversion rates, average processing times, error rates (FR-034)
  - Technical metrics: API response times, external service call latencies, cache hit rates, resource utilization (FR-035)
  - Prometheus.AspNetCore 8.2.1 for metrics at `/quotation/metrics`
  - Metrics tagged with service_name, version, region, environment
  - Analytics endpoints for conversion rates, turnaround time, channel performance (FR-022)

### ✅ XIII. .NET Aspire Integration (NON-NEGOTIABLE)
- **Status**: COMPLIANT
- **Evidence**:
  - `Maliev.Aspire.ServiceDefaults` consumed as NuGet package from GitHub Packages (PackageReference, not ProjectReference)
  - `nuget.config` in repository root with GitHub Packages source and credential placeholders
  - CI workflows use `GITOPS_PAT` with `read:packages` scope (not `GITHUB_TOKEN`)
  - Dockerfile uses BuildKit secrets for NuGet authentication (`--mount=type=secret,id=nuget_username`, `--mount=type=secret,id=nuget_password`)
  - `Program.cs` calls `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`

**GATE RESULT**: ✅ **ALL GATES PASSED** - Proceed to Phase 0 Research

## Project Structure

### Documentation (this feature)

```text
specs/001-quotation-rfq-service/
├── spec.md              # Feature specification (completed)
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (to be generated)
├── data-model.md        # Phase 1 output (to be generated)
├── quickstart.md        # Phase 1 output (to be generated)
├── contracts/           # Phase 1 output (to be generated)
│   ├── openapi.yaml
│   ├── rfq-endpoints.md
│   ├── quotation-endpoints.md
│   └── analytics-endpoints.md
├── checklists/
│   └── requirements.md  # Specification quality checklist (completed)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
Maliev.QuotationService/
├── Maliev.QuotationService.sln
├── nuget.config                         # GitHub Packages configuration
├── .gitignore
├── .dockerignore
│
├── Maliev.QuotationService.Api/
│   ├── Maliev.QuotationService.Api.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Controllers/
│   │   ├── v1/
│   │   │   ├── RfqController.cs
│   │   │   ├── QuotationController.cs
│   │   │   ├── AnalyticsController.cs
│   │   │   └── CustomerController.cs
│   │   └── HealthController.cs
│   ├── DTOs/
│   │   ├── Requests/
│   │   │   ├── CreateRfqRequest.cs
│   │   │   ├── UpdateRfqRequest.cs
│   │   │   ├── CreateQuotationRequest.cs
│   │   │   ├── UpdateQuotationRequest.cs
│   │   │   └── LinkCustomerRequest.cs
│   │   └── Responses/
│   │       ├── RfqResponse.cs
│   │       ├── QuotationResponse.cs
│   │       ├── AnalyticsResponse.cs
│   │       └── CustomerMatchResponse.cs
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   ├── IRfqService.cs
│   │   │   ├── IQuotationService.cs
│   │   │   ├── ICustomerMatchingService.cs
│   │   │   └── IAnalyticsService.cs
│   │   ├── RfqService.cs
│   │   ├── QuotationService.cs
│   │   ├── CustomerMatchingService.cs
│   │   └── AnalyticsService.cs
│   ├── ExternalClients/
│   │   ├── Interfaces/
│   │   │   ├── ICurrencyServiceClient.cs
│   │   │   ├── IMaterialServiceClient.cs
│   │   │   ├── IUploadServiceClient.cs
│   │   │   └── IPdfServiceClient.cs
│   │   ├── CurrencyServiceClient.cs
│   │   ├── MaterialServiceClient.cs
│   │   ├── UploadServiceClient.cs
│   │   └── PdfServiceClient.cs
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   └── RequestLoggingMiddleware.cs
│   ├── Validators/
│   │   ├── CreateRfqRequestValidator.cs
│   │   ├── UpdateRfqRequestValidator.cs
│   │   ├── CreateQuotationRequestValidator.cs
│   │   └── UpdateQuotationRequestValidator.cs
│   ├── Configuration/
│   │   ├── Settings/
│   │   │   ├── JwtSettings.cs
│   │   │   ├── RedisSettings.cs
│   │   │   ├── RabbitMQSettings.cs
│   │   │   └── ExternalServicesSettings.cs
│   │   └── Extensions/
│   │       ├── ServiceCollectionExtensions.cs
│   │       ├── AuthenticationExtensions.cs
│   │       ├── RateLimitingExtensions.cs
│   │       └── ObservabilityExtensions.cs
│   ├── Dockerfile
│   └── Properties/
│       └── launchSettings.json
│
├── Maliev.QuotationService.Data/
│   ├── Maliev.QuotationService.Data.csproj
│   ├── QuotationDbContext.cs
│   ├── Entities/
│   │   ├── Rfq.cs
│   │   ├── Quotation.cs
│   │   ├── QuotationVersion.cs
│   │   ├── QuotationLineItem.cs
│   │   ├── AuditLogEntry.cs
│   │   ├── InternalNote.cs
│   │   ├── Customer.cs
│   │   ├── MaterialReference.cs
│   │   ├── FileReference.cs
│   │   ├── DiscountStructure.cs
│   │   └── StaffRole.cs
│   ├── Configurations/
│   │   ├── RfqConfiguration.cs
│   │   ├── QuotationConfiguration.cs
│   │   ├── QuotationVersionConfiguration.cs
│   │   ├── QuotationLineItemConfiguration.cs
│   │   ├── AuditLogEntryConfiguration.cs
│   │   ├── InternalNoteConfiguration.cs
│   │   ├── CustomerConfiguration.cs
│   │   ├── MaterialReferenceConfiguration.cs
│   │   ├── FileReferenceConfiguration.cs
│   │   ├── DiscountStructureConfiguration.cs
│   │   └── StaffRoleConfiguration.cs
│   └── Migrations/
│       └── (EF Core migrations - generated, not committed initially)
│
└── Maliev.QuotationService.Tests/
    ├── Maliev.QuotationService.Tests.csproj
    ├── Fixtures/
    │   ├── IntegrationTestWebAppFactory.cs
    │   └── BaseIntegrationTest.cs
    ├── Unit/
    │   ├── Services/
    │   │   ├── RfqServiceTests.cs
    │   │   ├── QuotationServiceTests.cs
    │   │   ├── CustomerMatchingServiceTests.cs
    │   │   └── AnalyticsServiceTests.cs
    │   └── Validators/
    │       ├── CreateRfqRequestValidatorTests.cs
    │       ├── UpdateRfqRequestValidatorTests.cs
    │       ├── CreateQuotationRequestValidatorTests.cs
    │       └── UpdateQuotationRequestValidatorTests.cs
    ├── Integration/
    │   ├── RfqEndpointsTests.cs
    │   ├── QuotationEndpointsTests.cs
    │   ├── AnalyticsEndpointsTests.cs
    │   ├── CustomerMatchingTests.cs
    │   ├── RoleBasedAccessTests.cs
    │   ├── ExternalServiceResilienceTests.cs
    │   └── AuditTrailTests.cs
    └── Contract/
        ├── CurrencyServiceContractTests.cs
        ├── MaterialServiceContractTests.cs
        ├── UploadServiceContractTests.cs
        └── PdfServiceContractTests.cs
```

**Structure Decision**: Microservice architecture following MALIEV standard pattern with three projects:
1. **Api**: WebAPI project with controllers, DTOs, services, middleware, validators, external clients, and configuration
2. **Data**: Data layer with DbContext, entities, EF Core configurations, and migrations
3. **Tests**: Test project with unit tests, integration tests (using Testcontainers), and contract tests for external service interactions

This structure supports Clean Architecture (Controllers → Services → Data), stateless microservice design, and comprehensive test coverage with real infrastructure dependencies.

## Complexity Tracking

> **No violations - this table is empty**

All constitution requirements are met without justification needed. The service follows standard MALIEV microservice patterns with appropriate technology choices for the domain complexity.
