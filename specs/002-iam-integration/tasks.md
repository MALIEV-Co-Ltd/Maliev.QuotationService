# Tasks: Permission-Based Authorization Migration

**Input**: Design documents from `/specs/002-iam-integration/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md

**Tests**: Integration tests are mandatory as per Constitution III and implementation plan.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and definitions

- [X] T001 Define permission constants for all 16 operations in `Maliev.QuotationService.Api/Services/IAM/QuotationPermissions.cs`
- [X] T002 Define predefined roles mapping to permissions in `Maliev.QuotationService.Api/Services/IAM/QuotationPredefinedRoles.cs`
- [X] T003 [P] Update `AuditEntityType.cs` and `AuditActionType.cs` with `Security` and `Unauthorized` values
- [X] T004 Implement `QuotationIAMRegistrationService` to register roles and permissions with `IAMService` on startup in `Maliev.QuotationService.Api/Services/IAM/QuotationIAMRegistrationService.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

- [X] T005 Register Authorization Policies for each of the 16 permissions in `Maliev.QuotationService.Api/Configuration/Extensions/ServiceCollectionExtensions.cs`
- [X] T006 Implement `AuthorizationAuditMiddleware` to catch 403 Forbidden responses and log them to `AuditLogEntries` in `Maliev.QuotationService.Api/Middleware/AuthorizationAuditMiddleware.cs`
- [X] T007 [P] Configure `IIAMClient` for the registration service in `Maliev.QuotationService.Api/Configuration/Extensions/ServiceCollectionExtensions.cs`
- [X] T008 Implement `PermissionAuthorizationHandler` and `PermissionRequirement` in `Maliev.QuotationService.Api/Middleware/` (Moved to Services/IAM)

---

## Phase 3: User Story 1 - Quotation Lifecycle Management (Priority: P1) 🎯 MVP

**Goal**: Ensure Quotation Creators can create/send but only Managers can approve.

**Independent Test**: Verify that a JWT with `quotation-creator` role/permissions can create a quotation but fails to approve it (returning 403), while a `quotation-manager` can approve.

### Tests for User Story 1

- [X] T009 [P] [US1] Create integration tests for creator/manager flows using mock JWTs in `Maliev.QuotationService.Tests/Integration/AuthorizationTests.cs`

### Implementation for User Story 1

- [X] T010 [US1] Apply `[Authorize(Policy = "quotation.quotations.create")]` to create endpoint in `Maliev.QuotationService.Api/Controllers/v1/QuotationController.cs`
- [X] T011 [US1] Apply `[Authorize(Policy = "quotation.quotations.approve")]` to approve endpoint in `Maliev.QuotationService.Api/Controllers/v1/QuotationController.cs`
- [X] T012 [US1] Apply appropriate policies to line item endpoints in `Maliev.QuotationService.Api/Controllers/v1/QuotationController.cs`

**Checkpoint**: User Story 1 fully functional and testable independently.

---

## Phase 4: User Story 2 - Administrative Oversight (Priority: P1)

**Goal**: Allow Administrators full control including deletion and template management.

**Independent Test**: Verify that only a JWT with `quotation-admin` permissions can delete quotations and create templates.

### Tests for User Story 2

- [X] T013 [P] [US2] Add integration tests for admin-only delete and template operations in `Maliev.QuotationService.Tests/Integration/AuthorizationTests.cs`

### Implementation for User Story 2

- [X] T014 [US2] Apply `[Authorize(Policy = "quotation.quotations.delete")]` to delete endpoint in `Maliev.QuotationService.Api/Controllers/v1/QuotationController.cs`
- [X] T015 [US2] Apply `[Authorize(Policy = "quotation.templates.create")]` to template creation endpoints (N/A - No endpoints yet)

**Checkpoint**: User Story 2 functional and testable independently.

---

## Phase 5: User Story 3 - Quotation Visibility (Priority: P2)

**Goal**: Ensure Viewers have read-only access.

**Independent Test**: Verify that a JWT with `quotation-viewer` role/permissions can read but not update or create resources.

### Tests for User Story 3

- [X] T016 [P] [US3] Add integration tests for viewer read-only access in `Maliev.QuotationService.Tests/Integration/AuthorizationTests.cs`

### Implementation for User Story 3

- [X] T017 [US3] Apply `[Authorize(Policy = "quotation.quotations.read")]` to all GET endpoints in `QuotationController.cs` and `RfqController.cs`
- [X] T018 [US3] Ensure all line item and template GET endpoints require `read` permission (N/A - Embedded in Quotation)

**Checkpoint**: All user stories functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation

- [X] T019 Run all integration tests and verify zero warnings
- [X] T020 [P] Verify audit logs correctly capture the User ID and Action for failed attempts in `AuthorizationTests.cs`
- [X] T021 Run `quickstart.md` validation
