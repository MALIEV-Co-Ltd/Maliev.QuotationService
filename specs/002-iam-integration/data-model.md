# Data Model: IAM Integration

## Entities

The QuotationService does not maintain its own IAM entities (roles/permissions). It relies on the centralized `IAMService`.

### Security Auditing
The service uses the existing `AuditLogEntry` table for tracking unauthorized access attempts.

#### Updated Audit Enums
- **AuditEntityType**: Added `Security = 5`
- **AuditActionType**: Added `Unauthorized = 7`

## Relationships
- **Audit Log**: Every unauthorized attempt (403 Forbidden) is logged with the user's `SubjectId` (from JWT) and the target `EntityType`/`EntityId`.

## Validation Rules
- All incoming requests must contain a valid JWT with the necessary `permissions` claims.