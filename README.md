# Maliev Quotation Service

Sales quotation management system for the MALIEV platform, handling quote creation, pricing, approval workflows, and conversion to orders with full IAM integration.

## Service Description

The Quotation Service manages the complete sales quotation lifecycle from initial quote generation through approval, negotiation, and conversion to sales orders. It integrates with Customer, Material/Product, and Order services to provide a seamless quote-to-order process.

## Architecture Overview

### Project Structure
```
Maliev.QuotationService/
├── Maliev.QuotationService.Api/          # Presentation layer
│   ├── Controllers/v1/                   # Versioned REST API endpoints
│   ├── Services/                         # Business logic
│   └── Models/                           # DTOs
├── Maliev.QuotationService.Data/         # Data access layer
│   ├── Entities/                         # EF Core entities
│   └── Migrations/                       # Database migrations
└── Maliev.QuotationService.Tests/        # Integration tests
```

## Technologies Used

- **.NET 10.0** - Runtime and framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM with PostgreSQL provider
- **PostgreSQL 18** - Relational database
- **Redis** - Distributed caching for pricing
- **RabbitMQ** - Message queue via MassTransit
- **OpenTelemetry** - Observability

## Dependencies

### Databases
- **PostgreSQL**: Quotation headers, line items, pricing, approval workflow
- **Redis**: Pricing cache, quote drafts

### Messaging
- **RabbitMQ**: Events for quote approval, acceptance, conversion to orders

### External Services
- **IAM Service**: Authentication and authorization
- **Customer Service**: Customer information and credit limits
- **Material Service**: Product/material pricing and availability
- **Order Service**: Quote-to-order conversion
- **Upload Service**: Quote document attachments

## IAM Integration

### Required Permissions
- `quotations.read` - View quotations
- `quotations.write` - Create and update quotations
- `quotations.delete` - Delete draft quotations
- `quotations.approve` - Approve quotations (manager role)
- `quotations.send` - Send quotations to customers
- `quotations.convert` - Convert quotes to orders
- `quotations.pricing.override` - Override standard pricing

### Predefined Roles
- **Sales Representative**: Create and manage own quotations
- **Sales Manager**: Approve quotations and override pricing
- **Sales Director**: Full access including bulk operations

## API Endpoints

### Quotations
- `GET /v1/quotations` - List quotations (with filters)
- `POST /v1/quotations` - Create new quotation
- `GET /v1/quotations/{id}` - Get quotation details
- `PUT /v1/quotations/{id}` - Update quotation
- `DELETE /v1/quotations/{id}` - Delete draft quotation
- `POST /v1/quotations/{id}/submit` - Submit for approval
- `POST /v1/quotations/{id}/approve` - Approve quotation
- `POST /v1/quotations/{id}/reject` - Reject quotation
- `POST /v1/quotations/{id}/send` - Send to customer
- `POST /v1/quotations/{id}/convert-to-order` - Convert to sales order
- `GET /v1/quotations/{id}/pdf` - Generate PDF quote
- `GET /v1/quotations/customer/{customerId}` - Get customer quotations

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "QuotationDatabase": "Host=postgres;Port=5432;Database=maliev_quotations;Username=app;Password=secret",
    "Redis": "redis:6379"
  },
  "RabbitMQ": {
    "Host": "rabbitmq",
    "Username": "guest",
    "Password": "guest"
  },
  "Jwt": {
    "Key": "base64-encoded-key",
    "Issuer": "maliev-quotation-service",
    "Audience": "maliev-services"
  },
  "ExternalServices": {
    "IAM": {
      "BaseUrl": "http://iam-service:8080"
    },
    "Customer": {
      "BaseUrl": "http://customer-service:8080"
    },
    "Material": {
      "BaseUrl": "http://material-service:8080"
    },
    "Order": {
      "BaseUrl": "http://order-service:8080"
    },
    "Upload": {
      "BaseUrl": "http://upload-service:8080"
    }
  },
  "Quotation": {
    "DefaultValidityDays": 30,
    "RequireApprovalAboveAmount": 100000,
    "MaxDiscountPercentage": 15
  }
}
```

## Database

**PostgreSQL 18** with Entity Framework Core migrations.

**Main Tables:**
- `Quotations` - Quote headers (customer, dates, total, status)
- `QuotationItems` - Line items (product, quantity, price, discount)
- `QuotationApprovals` - Approval workflow tracking
- `QuotationVersions` - Version history for revisions
- `QuotationAttachments` - Associated documents

**Status Values:**
- Draft, Submitted, Approved, Rejected, Sent, Accepted, Converted, Expired

## Running the Service

### Development
```bash
cd Maliev.QuotationService.Api
dotnet run
```

**Access:**
- API: http://localhost:5000
- Health: http://localhost:5000/quotations/liveness
- Metrics: http://localhost:5000/quotations/metrics

### Docker
```bash
docker build -t maliev/quotation-service:latest .
docker run -p 8080:8080 maliev/quotation-service:latest
```

### Tests
```bash
dotnet test
```

## Test Status

**From Test Summary (2025-12-24):**
- **Status**: PASSED (No tests found)
- **Note**: Test infrastructure exists but no tests currently defined

**Recommended:** Add integration tests for:
- Quote creation and validation
- Approval workflow
- Pricing calculations
- Quote-to-order conversion

## Key Features

- **Quote Management**: Create, update, version quotations
- **Pricing Engine**: Automatic pricing from material service with discount support
- **Approval Workflow**: Configurable approval rules based on amount/discount
- **Quote Templates**: Reusable quote templates
- **Version Control**: Track quote revisions
- **PDF Generation**: Professional quote PDFs
- **Expiration Tracking**: Automatic quote expiration
- **Conversion**: Seamless quote-to-order conversion

## Events Published

- `QuotationCreatedEvent` - New quotation created
- `QuotationApprovedEvent` - Quotation approved
- `QuotationSentEvent` - Quotation sent to customer
- `QuotationAcceptedEvent` - Customer accepted quotation
- `QuotationConvertedEvent` - Converted to sales order
- `QuotationExpiredEvent` - Quotation expired

## Business Rules

1. **Approval Required**: Quotes above configured amount require manager approval
2. **Discount Limits**: Sales reps limited to maximum discount percentage
3. **Validity Period**: Quotes expire after configured days (default: 30)
4. **Price Lock**: Prices locked when quote is sent to customer
5. **Immutable**: Approved/sent quotes cannot be edited (must create revision)

## Support

- Test Summary: `B:\maliev\all-services-test-summary.txt`
- ServiceDefaults: `B:\maliev\Maliev.Aspire\Maliev.Aspire.ServiceDefaults\README.md`

## License

Proprietary - Copyright 2025 MALIEV Co., Ltd. All rights reserved.
