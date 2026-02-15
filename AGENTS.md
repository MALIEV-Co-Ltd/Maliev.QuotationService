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
- **Validation**: Validate inputs in Services or using FluentValidation if available.

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

## Workflow
1.  **Read**: Always read relevant files to understand context before editing.
2.  **Edit**: Make atomic changes.
3.  **Verify**: Run `dotnet build` and relevant tests after changes.
