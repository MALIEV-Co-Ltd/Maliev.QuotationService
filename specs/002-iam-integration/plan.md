# Implementation Plan: Permission-Based Authorization Migration

**Branch**: `002-iam-integration` | **Date**: 2025-12-23 | **Spec**: [specs/002-iam-integration/spec.md](spec.md)
**Input**: Feature specification from `/specs/002-iam-integration/spec.md`

## Summary

Migrate the QuotationService to a permission-based authorization model. The service will register 16 permissions and 4 standard roles with the central `IAMService` on startup. Authorization will be enforced locally by validating `permissions` and `roles` claims in the incoming JWT. All unauthorized attempts will be audited to the local database.

## Technical Context

**Language/Version**: .NET 10 (C#)
**Primary Dependencies**: ASP.NET Core, Entity Framework Core, Maliev.Aspire.ServiceDefaults
**Storage**: PostgreSQL (via EF Core) for audit logs
**Testing**: xUnit, Testcontainers
**Target Platform**: Linux (Docker)
**Project Type**: Single API service
**Performance Goals**: Authorization overhead < 5ms per request (claim validation)
**Constraints**: Zero warnings, strict separation of concerns, no AutoMapper/FluentValidation
**Scale/Scope**: Support for 16 permissions and cumulative roles for all quotation endpoints

## Constitution Check

- [x] Service Autonomy: Owns its domain logic and permissions definitions.
- [x] Explicit Contracts: Aligns with platform-wide `IAMService` and `AuthService` contracts.
- [x] Test-First Development: Integration tests with Testcontainers mandatory.
- [x] Real Infrastructure: PostgreSQL container required for testing.
- [x] Auditability: Auditing denied access attempts to `AuditLogEntries`.
- [x] Docker Best Practices: Dockerfile in API folder, .NET 10 base images.

## Project Structure

### Documentation (this feature)

```text
specs/002-iam-integration/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
Maliev.QuotationService.Api/
├── Configuration/
│   ├── Extensions/
│   └── Settings/
├── Controllers/
│   └── v1/
│       ├── QuotationController.cs (Update)
│       └── RfqController.cs (Update)
├── Middleware/
│   └── AuthorizationAuditMiddleware.cs (New)
├── Services/
│   ├── IAM/
│   │   ├── QuotationPermissions.cs (New - Constants)
│   │   ├── QuotationPredefinedRoles.cs (New - Mapping)
│   │   └── QuotationIAMRegistrationService.cs (New - Startup Registration)

Maliev.QuotationService.Tests/
├── Integration/
│   └── AuthorizationTests.cs (New)
```

**Structure Decision**: Standard flat structure as per Constitution XV. Projects are directly at root.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
