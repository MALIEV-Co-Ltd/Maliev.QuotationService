# Research: Permission-Based Authorization Migration

## Decision: Centralized IAM Registration
**Rationale**: Adheres to platform standards and Constitution I. The service owns its permission *definitions*, but `IAMService` owns the *assignments*. This prevents "Identity Silos" and simplifies management for administrators.
**Alternatives Considered**: Internal role mapping (rejected as it duplicates platform functionality and complicates cross-service authorization).

## Decision: Policy-Based Authorization via JWT Claims
**Rationale**: Using standard ASP.NET Core `AuthorizationPolicy` that checks for the presence of specific `permissions` claims is the fastest and most scalable approach. It avoids a database trip on every request.
**Alternatives Considered**: Calling IAM on every request (rejected due to latency).

## Decision: Startup Registration Pattern
**Rationale**: Following the pattern used in `AccountingService`, the service will register its permissions and roles with `IAMService` during its startup sequence. This ensures IAM always has the latest schema.
**Alternatives Considered**: Manual registration in IAM (rejected as error-prone during deployments).

## Permission Registry (16 Permissions)

| Category | Permission | Description |
|----------|------------|-------------|
| Quotation | `quotation.quotations.create` | Create new quotations |
| Quotation | `quotation.quotations.read` | Read quotation details |
| Quotation | `quotation.quotations.update` | Update quotation information |
| Quotation | `quotation.quotations.delete` | Delete quotations |
| Quotation | `quotation.quotations.approve` | Approve quotations |
| Quotation | `quotation.quotations.send` | Send quotations to customers |
| Quotation | `quotation.quotations.convert` | Convert quotation to order |
| Quotation | `quotation.quotations.revise` | Create revised versions |
| Quotation | `quotation.quotations.expire` | Mark quotations as expired |
| Line Item | `quotation.line-items.create` | Create line items |
| Line Item | `quotation.line-items.read` | Read line items |
| Line Item | `quotation.line-items.update` | Update line items |
| Line Item | `quotation.line-items.delete` | Delete line items |
| Template | `quotation.templates.create` | Create templates |
| Template | `quotation.templates.read` | Read templates |
| Template | `quotation.templates.use` | Use templates |

## Role Mapping (Centralized)

| Role | Permissions |
|------|-------------|
| `quotation-admin` | All 16 permissions |
| `quotation-manager` | create, read, update, approve, send, convert, revise, line-items.*, templates.* |
| `quotation-creator` | create, read, update, send, line-items.create, line-items.read, line-items.update, templates.read, templates.use |
| `quotation-viewer` | read, line-items.read, templates.read |