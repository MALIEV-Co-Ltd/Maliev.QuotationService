# Maliev.QuotationService

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/MALIEV-Co-Ltd/Maliev.QuotationService)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![Database](https://img.shields.io/badge/PostgreSQL-18-336791.svg)](https://www.postgresql.org/)
[![Tests](https://img.shields.io/badge/tests-39%2F39%20passing-brightgreen.svg)](https://github.com/MALIEV-Co-Ltd/Maliev.QuotationService)
[![License](https://img.shields.io/badge/license-Proprietary-red.svg)](LICENSE)

Sales quotation management system for the MALIEV platform. Handles quote creation, pricing calculations, multi-level approval workflows, customer negotiations, and seamless conversion to sales orders with full IAM integration and event-driven communication via MessagingContracts.

---

## Architecture & Tech Stack

### Technology Stack
- **.NET 10.0**: ASP.NET Core Web API with C# 13
- **PostgreSQL 18**: Primary database with Entity Framework Core 10.x
- **Redis**: Distributed caching for pricing and quote drafts
- **RabbitMQ**: Event-driven messaging via MassTransit 8.5.7
- **OpenTelemetry**: Structured logging, metrics, and distributed tracing
- **Testcontainers**: Integration testing with real PostgreSQL, Redis, RabbitMQ

### Project Structure
```
Maliev.QuotationService/
├── Maliev.QuotationService.Api/          # Presentation layer
│   ├── Controllers/v1/                   # Versioned REST API endpoints
│   ├── Services/                         # Business logic
│   ├── Models/                           # DTOs (Request/Response)
│   └── Consumers/                        # MassTransit event consumers
├── Maliev.QuotationService.Data/         # Data access layer
│   ├── Entities/                         # EF Core entities
│   ├── Configurations/                   # Entity configurations
│   └── Migrations/                       # Database migrations
└── Maliev.QuotationService.Tests/        # Integration tests
    ├── Integration/                      # API integration tests
    └── Testing/                          # Test infrastructure
```

### Dependencies

**Databases:**
- **PostgreSQL 18**: Quotation headers, line items, pricing, approval workflow, version history
- **Redis**: Pricing cache, quote drafts, frequently accessed customer quotes

**Messaging:**
- **RabbitMQ**: Event publishing (quotation lifecycle events) and consumption (customer/material updates)

**External Services:**
- **IAM Service**: Authentication, authorization, and permission management
- **Customer Service**: Customer information, credit limits, and account status
- **Material Service**: Product/material pricing, availability, and specifications
- **Order Service**: Quote-to-order conversion and order creation
- **Upload Service**: Quote document attachments (technical specs, drawings)

---

## ⚠️ Constitution Rules

These rules are **non-negotiable** and apply to ALL Maliev microservices:

### Banned Libraries
| ❌ BANNED | ✅ USE INSTEAD |
|-----------|----------------|
| AutoMapper | Explicit manual mapping |
| FluentValidation | Data Annotations (`[Required]`, `[StringLength]`, etc.) |
| FluentAssertions | xUnit `Assert.*` methods |
| In-memory test DB | Testcontainers (real PostgreSQL) |
| `/src` or `/tests` folders | Flat project structure at repo root |

### Mandatory Practices
- **No Secrets in Code**: All secrets injected via Google Secret Manager (environment variables)
- **TreatWarningsAsErrors**: Enabled in all `.csproj` files - zero warnings tolerated
- **XML Documentation**: Required on ALL public methods, properties, and classes
- **MessagingContracts Only**: ALL events use `Maliev.MessagingContracts` package (no local events)
- **ServiceDefaults Integration**: Use `Maliev.Aspire.ServiceDefaults` for infrastructure patterns

---

## Key Features

### Quotation Lifecycle Management
- **Quote Creation**: Generate quotes with automatic pricing from MaterialService
- **Pricing Engine**: Configurable discount rules, volume pricing, customer-specific pricing
- **Approval Workflow**: Multi-level approval based on amount thresholds and discount percentages
- **Customer Negotiation**: Track quote revisions and version history
- **Validity Management**: Automatic expiration tracking with configurable validity periods
- **Quote-to-Order Conversion**: Seamless conversion to sales orders with OrderService integration

### Quotation Workflow
```
Draft → Submitted → Approved/Rejected → Sent → Accepted/Expired → Converted to Order
                                              ↓
                                          Revised (new version)
```

**Status Transitions:**
- **Draft**: Quote created, editable
- **Submitted**: Awaiting approval (if above threshold)
- **Approved**: Ready to send to customer
- **Rejected**: Returned to sales rep for revision
- **Sent**: Transmitted to customer
- **Accepted**: Customer accepted quote
- **Expired**: Quote validity period exceeded
- **Converted**: Successfully converted to sales order
- **Revised**: New version created from existing quote

### Advanced Features
- **Quote Templates**: Reusable templates for common product configurations
- **Version Control**: Complete revision history with diff tracking
- **Discount Authorization**: Manager approval required for discounts exceeding limits
- **Price Locking**: Prices frozen when quote is sent to customer
- **Bulk Operations**: Create multiple quotes from template
- **PDF Generation**: Professional quote PDFs with company branding
- **Expiration Alerts**: Automated notifications before quote expiration
- **Win/Loss Analysis**: Track quote acceptance rates and reasons for rejection

### Pricing Engine
- **Automatic Pricing**: Fetch current prices from MaterialService
- **Volume Discounts**: Configurable quantity-based discounts
- **Customer-Specific Pricing**: Override pricing for strategic customers
- **Discount Limits**: Sales rep discount authority (default: 15%)
- **Margin Protection**: Minimum margin enforcement
- **Currency Support**: Multi-currency pricing with exchange rates

### Event-Driven Integration
- **Events Published** (via MessagingContracts):
  - `QuotationCreatedEvent` - New quotation created
  - `QuotationApprovedEvent` - Quotation approved for sending
  - `QuotationSentEvent` - Quotation sent to customer
  - `QuotationAcceptedEvent` - Customer accepted quotation
  - `QuotationConvertedEvent` - Converted to sales order
  - `QuotationExpiredEvent` - Quotation expired without acceptance
  - `QuotationRevisedEvent` - New version created

- **Events Consumed**:
  - `MaterialPriceChangedEvent` - Update pricing for draft quotes
  - `CustomerCreditLimitChangedEvent` - Validate quote totals against credit limits
  - `OrderCreatedEvent` - Confirm quote-to-order conversion success

---

## Quick Start

### Prerequisites
- .NET 10.0 SDK
- PostgreSQL 18 (local or via Kubernetes port-forward)
- Redis (optional, for caching)
- RabbitMQ (optional, for event messaging)

### Local Development

1. **Clone Repository**
   ```bash
   git clone https://github.com/MALIEV-Co-Ltd/Maliev.QuotationService.git
   cd Maliev.QuotationService
   ```

2. **Configure Database Connection**
   ```bash
   # Set connection string environment variable
   export ConnectionStrings__QuotationDbContext="Host=localhost;Port=5432;Database=quotation_app_db;Username=postgres;Password=<password>;"
   ```

3. **Apply Database Migrations**
   ```bash
   dotnet ef database update --project Maliev.QuotationService.Data
   ```

4. **Run the Service**
   ```bash
   cd Maliev.QuotationService.Api
   dotnet run
   ```

5. **Access API Documentation**
   - Scalar UI: http://localhost:5000/quotation/scalar
   - OpenAPI Spec: http://localhost:5000/quotation/openapi/v1.json
   - Health Check: http://localhost:5000/quotation/readiness

### Docker Deployment

```bash
# Build image
docker build -t maliev/quotation-service:latest .

# Run container
docker run -p 8080:8080 \
  -e ConnectionStrings__QuotationDbContext="Host=postgres;Port=5432;Database=quotation_app_db;..." \
  -e Jwt__PublicKey="<base64-encoded-public-key>" \
  maliev/quotation-service:latest
```

---

## API Endpoints

All endpoints are prefixed with `/quotation` (configured via `UsePathBase("/quotation")`):

### Quotations
- `GET /quotation/v1/quotations` - List quotations (paginated, filterable)
- `POST /quotation/v1/quotations` - Create new quotation
- `GET /quotation/v1/quotations/{id}` - Get quotation details
- `PUT /quotation/v1/quotations/{id}` - Update draft quotation
- `DELETE /quotation/v1/quotations/{id}` - Delete draft quotation
- `POST /quotation/v1/quotations/{id}/submit` - Submit for approval
- `POST /quotation/v1/quotations/{id}/approve` - Approve quotation
- `POST /quotation/v1/quotations/{id}/reject` - Reject quotation
- `POST /quotation/v1/quotations/{id}/send` - Send to customer
- `POST /quotation/v1/quotations/{id}/accept` - Mark as accepted by customer
- `POST /quotation/v1/quotations/{id}/revise` - Create new version
- `POST /quotation/v1/quotations/{id}/convert-to-order` - Convert to sales order
- `GET /quotation/v1/quotations/{id}/pdf` - Generate PDF quote
- `GET /quotation/v1/quotations/customer/{customerId}` - Get customer quotations
- `GET /quotation/v1/quotations/{id}/versions` - Get version history

### Quote Templates
- `GET /quotation/v1/templates` - List quote templates
- `POST /quotation/v1/templates` - Create template
- `GET /quotation/v1/templates/{id}` - Get template details
- `POST /quotation/v1/templates/{id}/generate-quote` - Create quote from template

### Pricing
- `POST /quotation/v1/pricing/calculate` - Calculate pricing for items
- `GET /quotation/v1/pricing/discounts` - Get available discount tiers
- `POST /quotation/v1/pricing/validate` - Validate pricing against rules

---

## Health & Monitoring

### Health Endpoints
- **Liveness**: `GET /quotation/liveness` - Service is running
- **Readiness**: `GET /quotation/readiness` - Service is ready (DB + dependencies healthy)

### Observability
- **Metrics**: Prometheus metrics at `/quotation/metrics`
- **Tracing**: OpenTelemetry distributed tracing to configured OTLP endpoint
- **Logging**: Structured logging with correlation IDs via ServiceDefaults

### Health Check Components
- PostgreSQL connection
- Redis cache availability
- RabbitMQ connection
- External service connectivity (IAM, Customer, Material, Order, Upload)

---

## Configuration

### Required Secrets (Google Secret Manager)
```
ConnectionStrings__QuotationDbContext - PostgreSQL connection string
Jwt__PublicKey  - Base64-encoded RSA-2048 public key (PEM format)
```

### Environment Variables
```bash
ConnectionStrings__QuotationDbContext="Host=postgres;Port=5432;Database=quotation_app_db;Username=app;Password=..."
ConnectionStrings__redis="redis:6379"
ConnectionStrings__rabbitmq="amqp://guest:guest@rabbitmq:5672"
Jwt__Issuer="https://dev.api.maliev.com/auth"
Jwt__Audience="https://dev.api.maliev.com"
ExternalServices__IAMService__BaseUrl="http://iam-service:8080"
ExternalServices__CustomerService__BaseUrl="http://customer-service:8080"
ExternalServices__MaterialService__BaseUrl="http://material-service:8080"
ExternalServices__OrderService__BaseUrl="http://order-service:8080"
ExternalServices__UploadService__BaseUrl="http://upload-service:8080"
```

### Business Rules Configuration
```json
{
  "Quotation": {
    "DefaultValidityDays": 30,
    "RequireApprovalAboveAmount": 100000,
    "MaxDiscountPercentage": 15,
    "MinMarginPercentage": 10,
    "AutoExpireQuotes": true,
    "AllowRevisionAfterSent": true
  }
}
```

### Configuration Files
- `appsettings.json` - Production settings (no secrets)
- `appsettings.Development.json` - Local development overrides
- `appsettings.Testing.json` - Test configuration with test keys

---

## IAM Integration

### Required Permissions
- `quotations.read` - View quotations
- `quotations.write` - Create and update quotations
- `quotations.delete` - Delete draft quotations
- `quotations.approve` - Approve quotations (manager role)
- `quotations.send` - Send quotations to customers
- `quotations.convert` - Convert quotes to orders
- `quotations.pricing.override` - Override standard pricing
- `quotations.pricing.approve` - Approve discount exceptions
- `quotations.templates.manage` - Manage quote templates

### Predefined Roles
- **Sales Representative**: Create and manage own quotations (`quotations.write`, `quotations.read`, `quotations.send`)
- **Sales Manager**: Approve quotations and pricing overrides (`quotations.approve`, `quotations.pricing.approve`, `quotations.*`)
- **Sales Director**: Full access including bulk operations and templates (`quotations.*`, `quotations.templates.manage`)
- **Finance**: View all quotations for pricing analysis (`quotations.read`)

---

## Testing

### Test Coverage
**39/39 integration tests passing (100%)**

Test suites:
- **QuotationsController Tests** (18 tests)
  - CRUD operations (create, read, update, delete)
  - Workflow transitions (submit, approve, reject, send, accept, convert)
  - Authorization checks (permission-based)
  - Version control and revisions
- **QuotationPricingController Tests** (9 tests)
  - Price calculations
  - Discount validation
  - Margin protection
  - Volume pricing
- **QuotationTemplatesController Tests** (7 tests)
  - Template creation and management
  - Quote generation from templates
- **QuotationConversionController Tests** (4 tests)
  - Quote-to-order conversion
  - Validation of customer credit limits
- **IAM Registration Tests** (1 test)
  - Permission registration with IAM service on startup

### Running Tests

```bash
# Run all tests
dotnet test Maliev.QuotationService.sln --verbosity normal

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~QuotationsControllerTests"
```

### Test Infrastructure
- **Testcontainers**: Real PostgreSQL 18, Redis, RabbitMQ containers
- **WireMock.Net**: Mock external services (IAM, Customer, Material, Order, Upload)
- **MassTransit Test Harness**: Verify event publishing and consumption
- **xUnit**: Test framework with `Assert.*` assertions

---

## Database

### Database Schema

**PostgreSQL 18** with Entity Framework Core migrations.

**Main Tables:**
- `Quotations` - Quote headers (customer, dates, total, status, approval info, validity period)
- `QuotationItems` - Line items (product, quantity, unit price, discount, subtotal)
- `QuotationApprovals` - Approval workflow tracking (approver, status, timestamp, comments)
- `QuotationVersions` - Version history for revisions (parent quote, version number, changes)
- `QuotationAttachments` - Associated documents (technical drawings, specifications)
- `QuotationTemplates` - Reusable quote templates

**Status Values:**
- `Draft`, `Submitted`, `Approved`, `Rejected`, `Sent`, `Accepted`, `Expired`, `Converted`, `Revised`

### Database Migrations

```bash
# Port forward to PostgreSQL pod (MUST use pod, not service)
kubectl port-forward -n <namespace> <postgres-pod> 5432:5432

# Set connection string environment variable
export ConnectionStrings__QuotationDbContext="Host=localhost;Port=5432;Database=quotation_app_db;Username=postgres;Password=<password>;"

# Create migration
dotnet ef migrations add MigrationName --project Maliev.QuotationService.Data

# Apply migration
dotnet ef database update --project Maliev.QuotationService.Data

# Rollback migration
dotnet ef database update PreviousMigrationName --project Maliev.QuotationService.Data
```

---

## Deployment

### Kubernetes Deployment

Service uses GitHub Actions workflows for automated deployment to development, staging, and production environments.

Deployments are managed via GitOps (ArgoCD).

### Port Forwarding

```bash
# Forward to service
kubectl port-forward -n <namespace> svc/<service-name> 8080:8080

# Forward to PostgreSQL (for migrations)
kubectl port-forward -n <namespace> <postgres-pod> 5432:5432

# Forward to Redis
kubectl port-forward -n <namespace> svc/redis 6379:6379
```

### Logs

```bash
# Tail logs
kubectl logs -f deployment/<service-name> -n <namespace>

# Get pod status
kubectl get pods -n <namespace> | grep <service-name>

# Describe pod
kubectl describe pod <pod-name> -n <namespace>
```

---

## Common Issues

### Issue: Tests fail with "Database connection string not configured"
**Solution**: Set `ConnectionStrings__QuotationDbContext` environment variable before running tests, or configure via User Secrets:
```bash
cd Maliev.QuotationService.Tests
dotnet user-secrets set "ConnectionStrings:QuotationDbContext" "Host=localhost;Port=5432;..."
```

### Issue: Migration fails with "Cannot connect to database"
**Solution**: Ensure PostgreSQL is accessible. If using Kubernetes, port-forward to the pod (NOT service):
```bash
kubectl port-forward -n <namespace> <postgres-pod> 5432:5432
```

### Issue: Scalar UI returns 404
**Solution**: Scalar is disabled in production. Check environment is Development or Staging.

### Issue: All JWT validations fail
**Solution**: Ensure `Jwt:PublicKey` is correctly configured in Google Secret Manager and matches the AuthService private key.

### Issue: Events not publishing
**Solution**: Verify RabbitMQ connection string is configured and MessagingContracts package is up-to-date.

### Issue: Pricing calculations incorrect
**Solution**: Ensure MaterialService is accessible and returning correct pricing. Check Redis cache for stale prices.

### Issue: Quote-to-order conversion fails
**Solution**: Verify OrderService is accessible and customer has sufficient credit limit.

---

## Development Guidelines

### Adding New Endpoints
1. Create request/response models in `Models/`
2. Add validators using Data Annotations
3. Implement service logic in `Services/`
4. Add controller action in `Controllers/v1/`
5. Write integration tests in `Tests/Integration/`
6. Update API documentation in README.md

### Adding New Database Entities
1. Create entity class in `Data/Entities/`
2. Add EF Core configuration in `Data/Configurations/`
3. Update `QuotationDbContext.cs` with DbSet
4. Create migration: `dotnet ef migrations add EntityName --project Maliev.QuotationService.Data`
5. Apply migration: `dotnet ef database update --project Maliev.QuotationService.Data`

### Code Style
- Follow .NET naming conventions
- Use async/await for all I/O operations
- Implement repository pattern for data access
- Use dependency injection for all services
- Add XML documentation comments for public APIs
- Include structured logging with correlation IDs

---

## Business Rules

1. **Approval Required**: Quotes above configured amount (`RequireApprovalAboveAmount`) require manager approval
2. **Discount Limits**: Sales representatives limited to maximum discount percentage (`MaxDiscountPercentage`)
3. **Margin Protection**: Minimum margin percentage enforced (`MinMarginPercentage`)
4. **Validity Period**: Quotes expire after configured days (`DefaultValidityDays`)
5. **Price Lock**: Prices locked when quote status changes to `Sent` (cannot be edited)
6. **Immutable Sent Quotes**: Approved/sent quotes cannot be edited (must create revision via `/revise` endpoint)
7. **Version Control**: All revisions maintain parent-child relationship with complete change history
8. **Credit Limit Check**: Quote-to-order conversion validates customer credit limit
9. **Material Availability**: Quote creation verifies material availability from MaterialService

---

## Support

- **CLAUDE.md**: Service-specific development guidelines
- **ServiceDefaults Documentation**: See Maliev.Aspire.ServiceDefaults repository
- **MessagingContracts**: See Maliev.MessagingContracts repository

---

## License

**Proprietary** - Copyright © 2025 MALIEV Co., Ltd. All rights reserved.
