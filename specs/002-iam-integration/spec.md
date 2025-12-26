# Feature Specification: Permission-Based Authorization Migration

**Feature Branch**: `002-iam-integration`  
**Created**: 2025-12-23  
**Status**: Draft  
**Input**: User description: "Permission-Based Authorization Migration"

## Clarifications

### Session 2025-12-23
- Q: How should internal roles be mapped to external users? → A: Centralized IAM: Roles are managed and assigned in the central `IAMService` and passed via JWT claims.
- Q: How should authorization decisions be audited? → A: Log Failures Only: Audit only denied access attempts to the local `AuditLogEntries` table.
- Q: Should users be allowed multiple roles? → A: Cumulative Permissions: Users can have multiple roles; permissions are unioned.
- Q: How should roles be assigned to users? → A: External Management: Use the `IAMService` UI or API for role assignment.
- Q: Can admins assign individual permissions or custom roles? → A: Role-only: Users are assigned the 4 predefined roles defined for the Quotation service.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Quotation Lifecycle Management (Priority: P1)

As a Quotation Creator, I want to create and send quotations to customers, while ensuring that only authorized Managers can approve them, so that we maintain quality control over our sales process.

**Why this priority**: Core business flow. Authorization is critical to ensure that only qualified staff can commit the company to a quotation.

**Independent Test**: Can be fully tested by attempting to create a quotation as a 'creator', then attempting to approve it (should fail), and finally having a 'manager' approve it (should succeed).

**Acceptance Scenarios**:

1. **Given** a user with 'quotation-creator' role, **When** they attempt to create a quotation, **Then** the quotation is successfully created.
2. **Given** a user with 'quotation-creator' role, **When** they attempt to approve a quotation, **Then** the system denies the request.
3. **Given** a user with 'quotation-manager' role, **When** they attempt to approve a quotation, **Then** the quotation status is updated to Approved.

---

### User Story 2 - Administrative Oversight (Priority: P1)

As an Administrator, I want full control over all quotation resources, including the ability to delete records and manage templates, so that I can maintain the integrity and configuration of the system.

**Why this priority**: Essential for system maintenance and handling exceptional cases (like data correction or cleanup).

**Independent Test**: Can be tested by verifying that only the 'admin' role can successfully call the delete endpoint and create new templates.

**Acceptance Scenarios**:

1. **Given** a user with 'quotation-admin' role, **When** they delete a quotation, **Then** the quotation is removed from the active system.
2. **Given** a user with 'quotation-creator' role, **When** they attempt to delete a quotation, **Then** the system denies the request.

---

### User Story 3 - Quotation Visibility (Priority: P2)

As a Viewer, I want to be able to see existing quotations and line items without being able to make any changes, so that I can stay informed about current offers without risk of accidental modification.

**Why this priority**: High value for transparency across departments (e.g., support or logistics) without compromising data security.

**Independent Test**: Can be tested by logging in as a 'viewer' and verifying read access works while all write operations return authorization errors.

**Acceptance Scenarios**:

1. **Given** a user with 'quotation-viewer' role, **When** they view a quotation list, **Then** they see the list of quotations.
2. **Given** a user with 'quotation-viewer' role, **When** they attempt to update a line item, **Then** the system denies the request.

---

### Edge Cases

- **Revocation**: What happens when a user's role is downgraded while they have an active session? (System should reflect permission changes on the next request or token refresh).
- **Deleted Templates**: How does the system handle attempts to 'use' a template that was deleted by an admin? (System should return a clear error indicating the resource is no longer available).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST define and register exactly 16 specific permissions for quotation, line-item, and template operations with the central `IAMService`.
- **FR-002**: System MUST define 4 standard roles: `quotation-admin`, `quotation-manager`, `quotation-creator`, and `quotation-viewer`.
- **FR-003**: System MUST enforce permission-based access control on all quotation API endpoints using standard JWT claims.
- **FR-004**: System MUST allow `quotation-manager` to approve quotations, which is restricted from `quotation-creator`.
- **FR-005**: System MUST allow `quotation-admin` to delete quotations, which is restricted from all other roles.
- **FR-006**: System MUST allow `quotation-creator` to use templates but NOT create them.
- **FR-007**: System MUST ensure that `quotation-viewer` has read-only access to all quotation-related resources.
- **FR-008**: System MUST register all predefined roles and their permission mappings with `IAMService` on startup.
- **FR-009**: System MUST audit and log all denied access attempts (unauthorized actions) to the local `AuditLogEntries` table with `EntityType.Security` and `ActionType.Unauthorized`.
- **FR-010**: System MUST support cumulative permissions (union of permissions from multiple roles provided in JWT).

### Key Entities *(include if feature involves data)*

- **Permission**: Represents a specific action allowed on a resource (e.g., `quotation.quotations.create`).
- **Role**: A collection of permissions defined centrally in `IAMService`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 16 defined permissions are correctly registered with `IAMService` and visible in the IAM dashboard.
- **SC-002**: All 4 standard roles are correctly mapped in `IAMService`.
- **SC-003**: 100% of unauthorized access attempts result in a `403 Forbidden` response and an audit log entry.
- **SC-004**: Performance overhead for authorization checks (validating claims) remains below 5ms per request.
