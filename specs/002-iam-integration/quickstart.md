# Quickstart: IAM Integration

## Development Environment Setup
1. Ensure Docker is running (for Testcontainers).
2. Run `dotnet restore`.

## Core Implementation Details
1. **Define Permissions**: Permission constants are defined in `QuotationPermissions.cs`.
2. **Define Roles**: Predefined roles and their permission mappings are in `QuotationPredefinedRoles.cs`.
3. **IAM Registration**: The service automatically registers permissions and roles with the central `IAMService` on startup via `QuotationIAMRegistrationService`.
4. **Authorization Logic**:
   - Uses standard ASP.NET Core `AuthorizationPolicy` with `PermissionPolicyProvider`.
   - Enforced via `[Authorize(Policy = QuotationPermissions.X)]` on controllers.
   - Validates `permissions` claims in the incoming JWT.
5. **Audit Logging**: `AuthorizationAuditMiddleware` logs all `403 Forbidden` events to the `AuditLogEntries` table with `EntityType.Security` and `ActionType.Unauthorized`.

## Verification Command
```bash
dotnet test Maliev.QuotationService.Tests --filter Category=Authorization
```