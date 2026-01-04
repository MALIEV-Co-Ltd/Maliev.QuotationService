# Maliev Quotation Service

[![Build Status](https://img.shields.io/badge/Build-Passing-success)](https://github.com/ORGANIZATION/Maliev.QuotationService)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Database](https://img.shields.io/badge/Database-PostgreSQL%2018-blue)](https://www.postgresql.org/)

Advanced sales quotation management system for the Maliev manufacturing ecosystem.

**Role in MALIEV Architecture**: The primary engine for sales negotiation. It handles the conversion of prospect inquiries into formal quotations by integrating with Material pricing and Customer data, orchestrating approvals, and ultimately enabling seamless transition to sales orders.

---

## 🏗️ Architecture & Tech Stack

- **Framework**: ASP.NET Core 10.0 (C# 13)
- **Database**: PostgreSQL 18 with Entity Framework Core 10.x
- **Distributed Cache**: Redis 7.x (Pricing transients & draft persistence)
- **Messaging**: RabbitMQ via MassTransit
- **Pricing Engine**: Rules-based logic for discounts, volume pricing, and margins
- **API Documentation**: OpenAPI 3.1 + Scalar UI
- **Observability**: OpenTelemetry (Metrics, Traces, Logging)

---

## ⚖️ Constitution Rules

This service strictly adheres to the platform development mandates:

### Banned Libraries
To maintain high performance and low complexity, the following are **NOT** used:
- ❌ **AutoMapper**: Explicit manual mapping only.
- ❌ **FluentValidation**: Standard Data Annotations (`[Required]`, `[EmailAddress]`) only.
- ❌ **FluentAssertions**: Standard xUnit `Assert` methods only.
- ❌ **In-memory Test DB**: All integration tests use **Testcontainers** with real PostgreSQL 18.

### Mandatory Practices
- ✅ **TreatWarningsAsErrors**: Enabled in all `.csproj` files.
- ✅ **XML Documentation**: Required on all public methods and properties.
- ✅ **No Secrets in Code**: All sensitive configuration injected via environment variables.
- ✅ **No Test Config in Program.cs**: Test configuration in test fixtures only.
- ✅ **IAM Integration**: Self-registers permissions with the IAM Service using GCP-style naming: `{service}.{resource}.{action}`.

---

## ✨ Key Features

- **Lifecycle Orchestration**: Complete management from Draft and Review to Sent, Accepted, and Converted-to-Order states.
- **Intelligent Pricing Engine**: Automated calculation incorporating material costs, volume discounts, and customer-specific tier pricing.
- **Revision Control**: Sophisticated versioning system that tracks quote iterations and negotiation history with differential analysis.
- **Margin Protection Framework**: Automated enforcement of minimum margin thresholds with mandatory manager overrides for exceptions.
- **Conversion Engine**: Seamless, one-click transition from accepted quotations to production orders with full data integrity.

---

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 SDK
- Docker Desktop (for infrastructure)
- PostgreSQL 18 (Alpine)

### Local Development Setup

1. **Clone the repository**
```bash
git clone https://github.com/ORGANIZATION/Maliev.QuotationService.git
cd Maliev.QuotationService
```

2. **Spin up Infrastructure**
```bash
docker run --name quotation-db -e POSTGRES_PASSWORD=YOUR_PASSWORD -p 5432:5432 -d postgres:18-alpine
docker run --name quotation-redis -p 6379:6379 -d redis:7-alpine
```

3. **Configure Environment**
```powershell
# Windows PowerShell
$env:ConnectionStrings__QuotationDbContext="YOUR_POSTGRES_CONNECTION_STRING"
$env:ConnectionStrings__Cache="YOUR_REDIS_CONNECTION_STRING"
```

4. **Apply Migrations & Run**
```bash
dotnet ef database update --project Maliev.QuotationService.Data
dotnet run --project Maliev.QuotationService.Api
```

The service will be available at `http://localhost:5000/quotation`. Access the interactive documentation at `http://localhost:5000/quotation/scalar`.

---

## 📡 API Endpoints

All endpoints are prefixed with `/quotation/v1/`.

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/quotations` | Create a new quotation draft |
| POST | `/quotations/{id}/submit` | Submit for managerial approval |
| POST | `/quotations/{id}/convert-to-order` | Finalize conversion to a production order |
| GET | `/templates` | Access reusable quote templates and standard configurations |

---

## 🏥 Health & Monitoring

Standardized health probes for Kubernetes orchestration:
- **Liveness**: `GET /quotation/liveness`
- **Readiness**: `GET /quotation/readiness` (Checks DB and Redis connectivity)
- **Metrics**: `GET /quotation/metrics` (Prometheus format)

---

## 🧪 Testing

We prioritize reliable tests over mock-heavy unit tests.

```bash
# Run all tests using Testcontainers
dotnet test --verbosity normal
```

- **Integration Tests**: Use real PostgreSQL 18 containers.
- **Contract Tests**: Ensure API stability for consumers.

---

## 📦 Deployment

Infrastructure management is handled via GitOps patterns.

- **Docker Image**: `REGION-docker.pkg.dev/PROJECT_ID/REPOSITORY/maliev-quotation-service:{sha}`
- **Environments**: Development, Staging, Production

---

## 📄 License

Proprietary - © 2025 MALIEV Co., Ltd. All rights reserved.
