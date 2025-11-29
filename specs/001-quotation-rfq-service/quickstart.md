# Quickstart Guide: Quotation Service Development

**Feature**: 001-quotation-rfq-service
**Date**: 2025-11-28

## Prerequisites

- **.NET 10 SDK** installed
- **Docker Desktop** running (for Testcontainers in tests)
- **Git** for version control
- **IDE**: Visual Studio 2025, VS Code with C# Dev Kit, or JetBrains Rider

## Repository Setup

### 1. Clone Repository

```bash
git clone https://github.com/MALIEV-Co-Ltd/Maliev.QuotationService.git
cd Maliev.QuotationService
git checkout 001-quotation-rfq-service
```

### 2. Verify Prerequisites

```bash
# Check .NET SDK version
dotnet --version  # Should be 10.0.x or higher

# Check Docker is running
docker info
```

## Local Development (Standalone Mode)

The service can run standalone without .NET Aspire, Redis, or RabbitMQ for development.

### 1. Restore Dependencies

```bash
dotnet restore Maliev.QuotationService.sln
```

**Note**: Requires GitHub Packages authentication. Set environment variables:

```bash
# Windows (PowerShell)
$env:NUGET_USERNAME="your-github-username"
$env:NUGET_PASSWORD="your-github-pat"  # Personal Access Token with read:packages scope

# Linux/macOS
export NUGET_USERNAME="your-github-username"
export NUGET_PASSWORD="your-github-pat"
```

### 2. Configure Standalone Development

`appsettings.Development.json` is pre-configured with:

- **Redis**: Disabled (uses in-memory cache)
- **RabbitMQ**: Disabled (uses in-memory transport)
- **PostgreSQL**: Uses local connection (must be running)

### 3. Start Local PostgreSQL (Docker)

```bash
docker run --name quotation-postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=quotation_app_db \
  -p 5432:5432 \
  -d postgres:18
```

### 4. Apply Database Migrations

```bash
cd Maliev.QuotationService.Api
dotnet ef database update --project ../Maliev.QuotationService.Data
```

### 5. Run the Service

```bash
# HTTP profile (recommended for development)
dotnet run --launch-profile http

# HTTPS profile (requires dev certificate)
dotnet run --launch-profile https
```

### 6. Access API Documentation

Open browser to:
- **HTTP**: `http://localhost:5272/quotation/scalar/v1`
- **HTTPS**: `https://localhost:7272/quotation/scalar/v1`

## Running with .NET Aspire (Full Orchestration)

For full microservices orchestration with Redis, RabbitMQ, and observability:

### 1. Clone Aspire Repository

```bash
cd ..
git clone https://github.com/MALIEV-Co-Ltd/Maliev.Aspire.git
cd Maliev.Aspire
```

### 2. Start Aspire AppHost

```bash
cd Maliev.Aspire.AppHost
dotnet run
```

This starts:
- All 20 MALIEV microservices
- PostgreSQL (all databases)
- RabbitMQ (message bus)
- Redis (distributed cache)
- Aspire Dashboard at `http://localhost:15000`

### 3. Access Quotation Service

- **API**: `http://localhost:5XXX/quotation/scalar/v1` (port assigned by Aspire)
- **Dashboard**: `http://localhost:15000` (view all services, logs, traces, metrics)

## Running Tests

### Unit Tests

```bash
dotnet test Maliev.QuotationService.Tests --filter "FullyQualifiedName~Unit"
```

### Integration Tests

**IMPORTANT**: Integration tests use Testcontainers, which requires Docker Desktop running.

```bash
# Ensure Docker is running
docker info

# Run integration tests (will start PostgreSQL, RabbitMQ, Redis containers automatically)
dotnet test Maliev.QuotationService.Tests --filter "FullyQualifiedName~Integration"
```

**What happens**:
1. IntegrationTestWebAppFactory starts 3 containers (PostgreSQL:18, RabbitMQ:3-management, Redis:7.0)
2. Migrations are applied automatically to test database
3. Tests run against real infrastructure
4. Containers are stopped and removed after tests complete

### All Tests

```bash
dotnet test Maliev.QuotationService.Tests
```

**Expected Duration**:
- Unit tests: ~5-10 seconds
- Integration tests: ~30-60 seconds (includes container startup)

## Building the Docker Image

### 1. Build with Docker

```bash
# From repository root
docker build \
  --secret id=nuget_username,env=NUGET_USERNAME \
  --secret id=nuget_password,env=NUGET_PASSWORD \
  -t maliev/quotation-service:dev \
  -f Maliev.QuotationService.Api/Dockerfile .
```

**Note**: Requires BuildKit secrets for NuGet authentication.

### 2. Run Docker Container

```bash
docker run --name quotation-service \
  -p 8080:8080 \
  -e ConnectionStrings__QuotationDbContext="Host=host.docker.internal;Port=5432;Database=quotation_app_db;Username=postgres;Password=postgres" \
  -e ASPNETCORE_ENVIRONMENT=Development \
  maliev/quotation-service:dev
```

### 3. Access Service

- **API**: `http://localhost:8080/quotation/scalar/v1`
- **Health**: `http://localhost:8080/quotation/liveness`
- **Metrics**: `http://localhost:8080/quotation/metrics`

## Development Workflow

### 1. Create Feature Branch

```bash
git checkout -b feature/add-customer-portal-rfq
```

### 2. Write Tests First (Test-First Development)

**Example: RFQ Creation Test**

```csharp
// Maliev.QuotationService.Tests/Integration/RfqEndpointsTests.cs
public class CreateRfq_Tests : BaseIntegrationTest
{
    [Fact]
    public async Task CreateRfq_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateRfqRequest
        {
            CustomerEmail = "test@example.com",
            CustomerName = "Test Customer",
            ChannelSource = RfqChannel.Website,
            RequestDetails = new { Message = "Need quote for 100 units" }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/quotation/v1/rfqs", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var rfq = await response.Content.ReadFromJsonAsync<RfqResponse>();
        rfq.Should().NotBeNull();
        rfq.Id.Should().NotBeEmpty();
    }
}
```

### 3. Run Test (should fail - RED)

```bash
dotnet test --filter "FullyQualifiedName~CreateRfq_ValidRequest_ReturnsCreated"
```

### 4. Implement Feature (GREEN)

```csharp
// Maliev.QuotationService.Api/Controllers/v1/RfqController.cs
[HttpPost]
[Authorize(Policy = "EmployeeOrHigher")]
public async Task<IActionResult> CreateRfq([FromBody] CreateRfqRequest request)
{
    var rfq = await _rfqService.CreateAsync(request);
    return CreatedAtAction(nameof(GetRfq), new { id = rfq.Id }, rfq);
}
```

### 5. Run Test Again (should pass)

```bash
dotnet test --filter "FullyQualifiedName~CreateRfq_ValidRequest_ReturnsCreated"
```

### 6. Refactor (if needed)

Clean up code while keeping tests passing.

### 7. Commit Changes

```bash
git add .
git commit -m "feat: add RFQ creation endpoint with validation"
git push origin feature/add-customer-portal-rfq
```

## Database Migrations

### Create New Migration

```bash
cd Maliev.QuotationService.Api

# Add new migration
dotnet ef migrations add AddQuotationApprovalWorkflow \
  --project ../Maliev.QuotationService.Data \
  --startup-project .

# Review generated migration in Maliev.QuotationService.Data/Migrations/

# Apply migration locally
dotnet ef database update --project ../Maliev.QuotationService.Data
```

### Remove Last Migration (if not applied)

```bash
dotnet ef migrations remove --project ../Maliev.QuotationService.Data
```

## Troubleshooting

### Issue: NuGet Restore Fails (401 Unauthorized)

**Solution**: Set NUGET_USERNAME and NUGET_PASSWORD environment variables with valid GitHub PAT.

```bash
# Check GitHub PAT has 'read:packages' scope
# Regenerate token if necessary at https://github.com/settings/tokens
```

### Issue: Testcontainers Tests Fail (Docker not running)

**Solution**: Ensure Docker Desktop is running:

```bash
docker info  # Should show Docker engine info, not error
```

### Issue: Database Connection Fails

**Solution**: Check PostgreSQL is running:

```bash
docker ps | grep quotation-postgres

# If not running, start it:
docker start quotation-postgres
```

### Issue: Port Already in Use (5272 or 7272)

**Solution**: Change port in `launchSettings.json` or kill existing process:

```bash
# Windows (PowerShell)
Get-Process -Id (Get-NetTCPConnection -LocalPort 5272).OwningProcess | Stop-Process

# Linux/macOS
lsof -ti:5272 | xargs kill -9
```

### Issue: Build Warnings

**Solution**: Fix all warnings immediately. Project has `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.

```bash
dotnet build  # Should show 0 warnings, 0 errors
```

## Useful Commands

```bash
# Watch mode (auto-rebuild on file changes)
dotnet watch run --project Maliev.QuotationService.Api

# Run specific test
dotnet test --filter "FullyQualifiedName~RfqServiceTests.CreateAsync_ValidRequest_CreatesRfq"

# Generate code coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Check for outdated packages
dotnet list package --outdated

# Clean build artifacts
dotnet clean
```

## Next Steps

1. **Review Specification**: Read `specs/001-quotation-rfq-service/spec.md` for requirements
2. **Review Data Model**: Read `specs/001-quotation-rfq-service/data-model.md` for entity structure
3. **Review API Contracts**: Read `specs/001-quotation-rfq-service/contracts/api-overview.md` for endpoint design
4. **Generate Tasks**: Run `/speckit.tasks` to generate implementation task breakdown

## Resources

- **MALIEV Constitution**: `.specify/memory/constitution.md` (architectural principles)
- **Plan**: `specs/001-quotation-rfq-service/plan.md` (implementation plan)
- **Research**: `specs/001-quotation-rfq-service/research.md` (technology decisions)
- **.NET 10 Docs**: https://learn.microsoft.com/en-us/dotnet/
- **Entity Framework Core**: https://learn.microsoft.com/en-us/ef/core/
- **Testcontainers**: https://dotnet.testcontainers.org/

## Support

- **Issues**: Create GitHub issue in Maliev.QuotationService repository
- **Questions**: Contact MALIEV architecture team
- **Documentation**: See Aspire Dashboard for live observability

---

**Happy Coding!** Remember: Write tests first, keep warnings at zero, and commit frequently.
