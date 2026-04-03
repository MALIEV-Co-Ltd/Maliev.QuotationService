# Agent Instructions for Maliev.QuotationService

> **Workspace root** `B:\maliev` contains **41 independent git repos**. Each `Maliev.*` folder is its own repo. Always work within the service directory.

---

## Build, Test & Lint Commands

All commands run from `B:\maliev\Maliev.QuotationService`.

```powershell
# Build (treats warnings as errors — all must be fixed)
dotnet build Maliev.QuotationService.slnx

# Run all tests
dotnet test Maliev.QuotationService.slnx --verbosity normal

# Run a single test method
dotnet test --filter "FullyQualifiedName~RfqServiceTests.CreateAsync_ShouldCreateRfq_WhenValidRequest"

# Run all tests in a class
dotnet test --filter "FullyQualifiedName~RfqServiceTests"

# Run with code coverage
dotnet test Maliev.QuotationService.slnx --collect:"XPlat Code Coverage"

# Format check
dotnet format Maliev.QuotationService.slnx

# EF Core migrations (Infrastructure project only)
dotnet ef migrations add <Name> --project Maliev.QuotationService.Infrastructure --startup-project Maliev.QuotationService.Infrastructure
```

---

## Code Style & Conventions

### Workspace Structure

```
Maliev.QuotationService/
├── Maliev.QuotationService.Api/              # Controllers, Consumers, Middleware
├── Maliev.QuotationService.Application/      # Use cases, DTOs, Interfaces, Handlers
├── Maliev.QuotationService.Domain/           # Entities, value objects, domain interfaces
├── Maliev.QuotationService.Data/             # (Legacy) EF Core Entities, DbContext
├── Maliev.QuotationService.Infrastructure/   # EF Core DbContext, repositories, HTTP clients, Migrations
├── Maliev.QuotationService.Tests/            # Unit + Integration tests (xUnit)
├── Directory.Build.props                     # Central package versioning
└── Maliev.QuotationService.slnx             # Solution file (.slnx preferred over .sln)
```

### C# Naming & Formatting
- **Framework**: .NET 10.0, Latest C#
- **Namespaces**: File-scoped (`namespace Maliev.QuotationService.Domain.Entities;`)
- **Classes/Methods/Properties**: `PascalCase`
- **Private fields**: `_camelCase` (underscore prefix)
- **Parameters/locals**: `camelCase`
- **Async methods**: Suffix with `Async` (e.g., `GetByIdAsync`)
- **Interfaces**: Prefix with `I` (e.g., `IRfqService`)
- **Permissions**: GCP-style `{domain}.{plural-resource}.{action}` as `public const string` in a `Permissions` static class
  - Valid: `quotation.rfqs.create`, `quotation.quotes.approve`
  - Invalid: `quotation.rfq.create` (singular), `quotation.create` (missing resource)
- **XML docs**: Required on ALL public methods and properties
- **Nullable**: Enabled (`<Nullable>enable</Nullable>`). Use `?` explicitly
- **Imports**: System first, then third-party, then local. Alphabetize within groups. Remove unused `using`
- **Braces**: Allman style (new line) for methods and control structures. Expression-bodied for properties/accessors
- **Indentation**: 4 spaces, LF line endings, UTF-8, trim trailing whitespace

### C# Patterns
- **DI**: Constructor injection with `private readonly` fields
- **Controllers**: `[ApiController]`, `[ApiVersion("1")]`, `[Route("quotation/v{version:apiVersion}")]`
- **Logging**: `ILogger<T>` with structured placeholders (never interpolate): `_logger.LogInformation("Processing {RfqId}", rfqId)`
- **Error handling**: Global exception middleware. Return `ProblemDetails` / `ErrorResponse` DTOs. Never expose stack traces
- **JSON**: Check existing conventions in this service for naming policy
- **Manual mapping**: Static extension methods (`ToDto()`, `ToEntity()`). AutoMapper is banned
- **Validation**: `System.ComponentModel.DataAnnotations` on DTOs. FluentValidation is banned
- **Data Access**: Use Entity Framework Core. Current pattern favors direct `DbContext` in services.

---

## Banned Libraries (Build Will Fail)

| Banned | Use Instead |
|--------|-------------|
| AutoMapper | Manual mapping extensions |
| FluentValidation | DataAnnotations or manual validation |
| FluentAssertions | Standard xUnit `Assert.*` |
| Swashbuckle/Swagger | Scalar (at `/quotation/scalar`) |
| InMemoryDatabase (EF Core) | Testcontainers with real PostgreSQL |

---

## Testing Rules

- **Framework**: xUnit with standard `Assert` (`Assert.Equal`, `Assert.NotNull`, etc.)
- **Mocking**: Moq for unit tests
- **Naming**: `MethodName_StateUnderTest_ExpectedBehavior` or `HTTP_METHOD_Path_Scenario_ExpectedStatus`
- **Coverage**: Minimum 80% per service
- **Integration tests**: `BaseIntegrationTestFactory<TProgram, TDbContext>` with Testcontainers (PostgreSQL, Redis, RabbitMQ). Never InMemoryDatabase. Use `IntegrationTestWebAppFactory` for WebApplicationFactory setup.
- **System tests** (Tier 3): `AspireTestFixture` with `[Collection("AspireDomainTests")]` — shared AppHost, never one per class
- **Eventual consistency**: Use `TestHelpers.WaitForAsync`. Never `Task.Delay`
- **MassTransit consumers**: Must have consumer tests using `AddMassTransitTestHarness()`
- Use `[Fact]` for single cases, `[Theory]` for parameterized tests

### Testing Strategy (4-Tier Pyramid Context)

This service's tests cover **Tier 1 (Unit)** and **Tier 2 (Service Integration)** of the Maliev testing pyramid:

| Tier | What to Test | Infrastructure |
|------|-------------|---------------|
| **Unit** | Business logic, domain models, service methods with mocked dependencies | None (mocks only) |
| **Service Integration** | API endpoints, database persistence, permission enforcement, input validation | `BaseIntegrationTestFactory` + Testcontainers (Postgres/Redis/RabbitMQ) |

**Tier 3 (System Integration)** — cross-service workflows and event chains — is tested in `Maliev.Aspire.Tests/`.

> Full ecosystem test strategy: `Maliev.Aspire.Tests/TEST_PLAN.md`

---

## Mandatory Rules

- **`TreatWarningsAsErrors = true`**: Zero warnings allowed. No suppression
- **`[RequirePermission("quotation.resources.action")]`**: On all endpoints, not plain `[Authorize]`
- **API versioning**: All routes versioned (`v1/`)
- **Service prefix**: Routes prefixed with `/quotation`
- **Scalar docs**: Configured at `/quotation/scalar`
- **Secrets**: Never hardcoded. Use GCP Secret Manager or environment variables
- **Async/await**: All the way down. Pass `CancellationToken`
- **EF Core Design package**: Only in Infrastructure project, never in Api
- **PostgreSQL xmin**: Shadow property only — `entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion()`. Never add entity property
- **Temporary files**: Generate in `/temp` folder, clean up afterwards
- **DTOs**: Use specific Request/Response DTOs. Do not expose Entities directly in API endpoints

---

## Workflow
1. **Read**: Always read relevant files to understand context before editing.
2. **Edit**: Make atomic changes.
3. **Verify**: Run `dotnet build Maliev.QuotationService.slnx` and relevant tests after changes.

---

## Git Rules

- Each `Maliev.*` folder is an independent git repo. `cd` into it before git commands
- **Commit early and often** after every meaningful unit of work. Do not accumulate changes
- **Never use `git checkout` to restore files** — commit first, then `git revert` or `git reset --soft`
- Feature branches merged to `develop` via PR. Do not push without being asked

---

## Database & EF Core — Mandatory Rules

### EF Core Design Package
- `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- It belongs ONLY in the Infrastructure (or Data) project where migrations live
- Migration commands must target Infrastructure as both project and startup-project:
  ```
  dotnet ef migrations add <Name> --project Maliev.QuotationService.Infrastructure --startup-project Maliev.QuotationService.Infrastructure
  ```

### PostgreSQL xmin Concurrency — Mandatory Pattern
Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- Never use `.Ignore(e => e.Xmin)` — remove the entity property instead
