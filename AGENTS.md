# Agent Instructions for Maliev.QuotationService

## Build & Test Commands

### Build
Run from the solution root:
```bash
dotnet build
```

### Test
Run all tests:
```bash
dotnet test
```

Run a single test (xUnit):
```bash
dotnet test --filter "FullyQualifiedName=Maliev.QuotationService.Tests.Unit.Services.RfqServiceTests.CreateAsync_ShouldCreateRfq_WhenValidRequest"
```
*Note: Replace the fully qualified name with the specific test method you want to run.*

### Lint/Format
Format code according to .NET standards:
```bash
dotnet format
```

## Code Style Guidelines

### General
- **Framework**: .NET 10.0
- **Language Version**: Latest C#
- **Nullable**: Enabled (`<Nullable>enable</Nullable>`). Handle nullability explicitly.
- **Async/Await**: Use `async/await` for all I/O bound operations. Always pass `CancellationToken` to async methods where supported.
- **Namespaces**: Use file-scoped namespaces (e.g., `namespace Maliev.QuotationService.Api.Services;`).

### Naming Conventions
- **Classes/Methods/Properties**: PascalCase.
- **Parameters/Locals**: camelCase.
- **Private Fields**: _camelCase (e.g., `private readonly QuotationDbContext _context;`).
- **Interfaces**: Prefix with `I` (e.g., `IRfqService`).
- **Async Methods**: Suffix with `Async` (e.g., `GetByIdAsync`).

### Formatting
- **Indentation**: 4 spaces.
- **Braces**: Allman style (braces on new lines).
- **Usings**: Place `using` directives at the top of the file. Remove unused usings.

### Architecture & Patterns
- **Layering**:
  - `Api`: Controllers, Middleware, DTOs, Application Services.
  - `Data`: EF Core Entities, DbContext, Migrations.
  - `Tests`: Unit and Integration tests.
- **Dependency Injection**: Use constructor injection. Register services in `Program.cs` or extension methods.
- **Data Access**: Use Entity Framework Core. Use `DbContext` directly or via Repositories (current pattern favors direct DbContext in Services).
- **DTOs**: Use specific Request/Response DTOs. Do not expose Entities directly in API endpoints.
- **Logging**: Use `ILogger<T>` injected into the constructor.
- **Validation**: Validate inputs in Services using Data Annotations (`[Required]`, `[EmailAddress]`) or manual validation.

### Error Handling
- Use global exception handling middleware (Standard Middleware).
- Throw specific exceptions (e.g., `KeyNotFoundException` for missing resources).
- Log exceptions with context before throwing or returning error responses.

### Documentation
- Use XML documentation (`///`) for public methods and classes, especially in Services and API contracts.

### Testing
- **Framework**: xUnit.
- **Mocking**: Moq.
- **Integration Tests**: Use `WebApplicationFactory` (via `IntegrationTestWebAppFactory`).
- **Naming**: `MethodName_StateUnderTest_ExpectedBehavior`.

### Testing Strategy (4-Tier Pyramid Context)

This service's tests cover **Tier 1 (Unit)** and **Tier 2 (Service Integration)** of the Maliev testing pyramid:

| Tier | What to Test | Infrastructure |
|------|-------------|---------------|
| **Unit** | Business logic, domain models, service methods with mocked dependencies | None (mocks only) |
| **Service Integration** | API endpoints, database persistence, permission enforcement, input validation | `BaseIntegrationTestFactory` + Testcontainers (Postgres/Redis/RabbitMQ) |

**Tier 3 (System Integration)** — cross-service workflows and event chains — is tested in `Maliev.Aspire.Tests/`.

#### Key Rules
- Use `BaseIntegrationTestFactory<TProgram, TDbContext>` for integration tests (real Testcontainers, never InMemoryDatabase)
- Every MassTransit consumer MUST have a consumer test using `services.AddMassTransitTestHarness()`
- Test naming: `MethodName_StateUnderTest_ExpectedBehavior`
- Minimum 80% code coverage
- Use `[Fact]` for single cases, `[Theory]` for parameterized tests

> Full ecosystem test strategy: `Maliev.Aspire.Tests/TEST_PLAN.md`

## Workflow
1.  **Read**: Always read relevant files to understand context before editing.
2.  **Edit**: Make atomic changes.
3.  **Verify**: Run `dotnet build` and relevant tests after changes.


## Git & Version Control — Mandatory Rules

### 🚨 CRITICAL: Always Commit Code Changes (Non-Negotiable)
- **You MUST commit your changes to the local repository after completing any meaningful unit of work.**
- **Never accumulate uncommitted changes.** Do not wait until end of session or until something breaks.
- **Commit early and often** — if a change is meaningful (even a small fix or refactor), commit it.
- **You do NOT need to push to remote** — local commits are sufficient to protect against accidental loss.
- **If you are unsure whether to commit, commit anyway.** Extra commits are harmless; lost work is irreversible.
- This rule applies even if you are just "testing" or "exploring" — use git branches to isolate experimental work and commit those changes too.

### 🚨 CRITICAL: Never Use `git checkout` to Restore Broken Files
- **NEVER use `git checkout` to restore or recover files.** This operation discards uncommitted changes permanently and will result in data loss.
- **To undo/recover from broken files: first commit your current changes, then use `git revert` or `git reset --soft` to safely undo.**

## Database & EF Core — Mandatory Rules

### EF Core Design Package
- ❌ `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- ✅ It belongs ONLY in the Infrastructure (or Data) project where migrations live
- Migration commands must target Infrastructure as both project and startup-project (since EF Core Design package is in Infrastructure):
  ```
  dotnet ef migrations add <Name> --project Maliev.<Domain>Service.Infrastructure --startup-project Maliev.<Domain>Service.Infrastructure
  ```

### PostgreSQL xmin Concurrency — Mandatory Pattern
Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- ❌ Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- ❌ Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- ❌ Never use `.Ignore(e => e.Xmin)` — remove the entity property instead
