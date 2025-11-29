# Feature Specification: Unified Quotation and RFQ Management Service

**Feature Branch**: `001-quotation-rfq-service`
**Created**: 2025-11-28
**Status**: Draft
**Input**: User description: "The Quotation Service is responsible for the complete lifecycle of customer quotations within the MALIEV microservices ecosystem. This service replaces the older split architecture by fully merging the existing "quotation service" and "quotation request service" into a unified domain. It manages both inbound RFQs from any channel and the quotations generated internally by MALIEV staff. Every request for a quotation, regardless of origin, must be captured and stored in a single consistent domain model. The service must accept RFQs originating from diverse channels such as the website RFQ form, LINE, WhatsApp, Facebook Messenger, Instagram, email, and in-store walk-ins. Each RFQ must retain metadata describing the channel source, customer identification, any uploaded files or references to files stored by other services, timestamps, progress notes, and the evolving RFQ status. All RFQs must remain queryable for analytics, workflow monitoring, and reporting. The service must provide an internal workflow for updating RFQ details, recording internal notes, assigning RFQs to staff, and marking whether an RFQ eventually becomes a finalized quotation.

The service is also responsible for creating and managing all quotations issued to customers. Each quotation is generated either directly from an RFQ or created independently when needed. When a quotation is created from an RFQ, the service must link them so that the RFQ can be tracked from initial request through to quotation completion. A quotation must contain line items, quantities, material selections, manufacturing processes, pricing data, delivery expectations, validity periods, discount structures, customer information, and all business rules required by the MALIEV workflow. Quotations must support versioning so that staff can revise prices or details while retaining full history. The service must enforce clear state transitions for a quotation, including drafting, customer review, expiration, acceptance, cancellation, and any necessary internal approvals.

The Quotation Service must integrate with other microservices strictly through API contracts. Currency conversions and lists of available currencies must be obtained from the Currency Service when needed. Material data, mechanical properties, supported manufacturing processes, and related attributes must be obtained from the Material Service. File uploads must be handled by the Upload Service, and the Quotation Service must store only references or identifiers for these external assets. The Quotation Service must not perform PDF generation, as that responsibility belongs to the dedicated PDF Service. The service should provide structured payloads suitable for PDF generation, while leaving all rendering outside of its domain.

The service must provide full auditing capabilities. Every change to an RFQ or quotation must be recorded, including user identity, timestamp, action type, and changed fields. This data is essential for tracking business performance, sales effectiveness, and RFQ-to-quote conversion metrics. The service must expose endpoints enabling MALIEV staff to search, filter, and analyze RFQs and quotations by customer, channel, material type, date ranges, response times, conversion rates, or any other business-critical criteria. This unified architecture must support long-term analytics, including evaluating which channels produce the highest conversion rates, measuring average quotation turnaround time, identifying abandoned requests, and assessing pricing strategy performance.

The Quotation Service must be designed for responsiveness, maintainability, and data consistency. It should rely on efficient caching layers for high-frequency read operations but maintain database-backed durability for all mutations. The service must enforce boundaries so that RFQ logic and quotation logic remain internally separated yet fully integrated within a single microservice. This design must simplify the overall architecture while improving tracking, reducing duplication, minimizing operational overhead, and aligning the service with MALIEV's long-term business requirements."

## Clarifications

### Session 2025-11-28

- Q: Should different staff roles have different permissions for RFQ/quotation operations? → A: Yes, implement role-based permissions (e.g., sales staff can create/edit, managers can approve, analysts can view analytics)
- Q: How long should the system retain RFQ, quotation, and audit log data? → A: 7 years for all data (standard business record retention)
- Q: What observability capabilities should the service provide for monitoring and troubleshooting? → A: Standard observability (structured logging, key business/technical metrics, distributed tracing for external service calls)
- Q: How should the system identify and link customers across different channels? → A: Manual staff linking with assisted matching (system suggests potential matches, staff confirms)
- Q: What failure recovery strategy should the system use for external service calls? → A: Retry with exponential backoff and circuit breaker (3 retries with backoff, open circuit after threshold, auto-recovery)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Multi-Channel RFQ Intake and Tracking (Priority: P1)

MALIEV staff must capture and track all customer requests for quotations arriving from diverse channels including website forms, messaging platforms (LINE, WhatsApp, Facebook Messenger, Instagram), email, and in-store interactions. Each RFQ must preserve channel source information, customer identity, file references, timestamps, and current status. Staff must update RFQ details, add internal notes, assign RFQs to team members, and eventually convert qualified RFQs into formal quotations.

**Why this priority**: This represents the entry point for all quotation-related business activity. Without reliable RFQ capture and tracking, downstream quotation generation cannot function properly. This is the foundation of the unified service.

**Independent Test**: Can be fully tested by submitting RFQs through multiple channels (website, LINE, email) and verifying that staff can view, update, add notes, assign, and track each RFQ independently. Delivers immediate value by consolidating all RFQ sources into a single queryable system.

**Acceptance Scenarios**:

1. **Given** a customer submits an RFQ via the website form, **When** the RFQ is received, **Then** it is stored with channel source "website", customer identification, timestamp, and status "new"
2. **Given** an RFQ arrives from WhatsApp with attached files, **When** the RFQ is processed, **Then** it stores file references from the Upload Service and retains WhatsApp as the source channel
3. **Given** staff views an RFQ list, **When** filtering by channel source, **Then** only RFQs from that channel are displayed
4. **Given** staff opens an RFQ detail, **When** they add an internal note, **Then** the note is saved with timestamp and user identity
5. **Given** staff assigns an RFQ to a team member, **When** the assignment is saved, **Then** the assigned user can view the RFQ in their assigned list
6. **Given** multiple RFQs exist, **When** staff searches by customer name or date range, **Then** matching RFQs are returned with all metadata
7. **Given** an RFQ is in progress, **When** staff updates the status to "qualified", **Then** the RFQ status changes and is recorded in the audit log
8. **Given** a new RFQ arrives with customer contact information, **When** the system calculates match confidence using weighted scoring (exact email match = 100 points, exact phone match = 90 points, fuzzy name match = 70 points if Levenshtein distance < 3 edits), **Then** suggested matches with confidence ≥ 60% are displayed to staff (maximum 5 suggestions), sorted by confidence descending, showing matching fields (email, phone, name)
9. **Given** staff reviews suggested customer matches, **When** they confirm a match, **Then** the new RFQ is linked to the existing customer record and all historical RFQs/quotations for that customer become visible in a unified view
10. **Given** customer records are linked across channels, **When** staff views the customer profile, **Then** all RFQs and quotations from all channels are displayed with their original channel source preserved

---

### User Story 2 - Quotation Creation and Versioning (Priority: P1)

MALIEV staff must create quotations either from an existing RFQ or independently. Each quotation must include line items with quantities, material selections, manufacturing processes, pricing, delivery expectations, validity periods, and discount structures. Staff must be able to revise quotations while retaining full version history, supporting multiple iterations before finalization.

**Why this priority**: Quotation generation is the core business function. This story delivers the ability to create and manage quotations, which directly generates revenue. Without this, the service provides no business value.

**Independent Test**: Can be fully tested by creating a quotation from an RFQ, adding line items with materials and pricing, saving multiple versions, and verifying that history is preserved. Delivers immediate value by enabling staff to issue quotations to customers.

**Acceptance Scenarios**:

1. **Given** an RFQ marked as "qualified", **When** staff creates a quotation from the RFQ, **Then** the quotation is linked to the RFQ and retains customer information
2. **Given** staff creates a new quotation without an RFQ, **When** customer information is provided, **Then** the quotation is created independently
3. **Given** a quotation is being drafted, **When** staff adds a line item with material selection, quantity, and pricing, **Then** the line item is stored and displayed in the quotation
4. **Given** a quotation exists, **When** staff revises the pricing or line items, **Then** a new version is created and the previous version is preserved in history
5. **Given** a quotation has multiple versions, **When** staff views version history, **Then** all versions are displayed with timestamps and change summaries
6. **Given** a quotation is complete, **When** staff sets the validity period and discount structure, **Then** these details are stored and available for review
7. **Given** a quotation is ready for customer review, **When** staff changes status to "sent to customer", **Then** the quotation status is updated and recorded in the audit log

---

### User Story 3 - Quotation State Management and Lifecycle (Priority: P2)

MALIEV staff must manage quotations through clear state transitions including draft, customer review, expiration, acceptance, cancellation, and internal approval. The system must enforce valid state transitions and record every status change for audit purposes.

**Why this priority**: State management ensures quotations follow the correct business workflow and prevents invalid operations. This is essential for business process compliance but can be implemented after basic quotation creation is working.

**Independent Test**: Can be fully tested by creating a quotation, transitioning through various states (draft → sent → accepted or cancelled), and verifying that invalid transitions are prevented and all changes are audited. Delivers value by ensuring quotation workflow integrity.

**Acceptance Scenarios**:

1. **Given** a new quotation is created, **When** it is saved, **Then** the initial state is "draft"
2. **Given** a quotation is in "draft" state, **When** staff sends it to the customer, **Then** the state changes to "customer review"
3. **Given** a quotation is in "customer review" state, **When** the validity period expires, **Then** the state automatically changes to "expired"
4. **Given** a quotation is in "customer review" state, **When** the customer accepts, **Then** the state changes to "accepted"
5. **Given** a quotation is in "customer review" state, **When** the customer or staff cancels, **Then** the state changes to "cancelled"
6. **Given** a quotation requires internal approval, **When** staff submits for approval, **Then** the state changes to "pending approval"
7. **Given** a quotation is in "pending approval" state, **When** approval is granted, **Then** the state changes to "approved" and can proceed to "customer review"
8. **Given** any state transition occurs, **When** the change is saved, **Then** the user, timestamp, and previous/new states are recorded in the audit log
9. **Given** a sales staff member attempts to approve a quotation, **When** the approval action is attempted, **Then** the system rejects the operation with an authorization error indicating manager role is required
10. **Given** an analyst attempts to edit a quotation, **When** the edit action is attempted, **Then** the system rejects the operation with an authorization error indicating insufficient permissions

---

### User Story 4 - External Service Integration (Priority: P2)

MALIEV staff and customers must work with quotations that include accurate currency conversions, material properties, and uploaded documents. The service must obtain currency data from the Currency Service, material information from the Material Service, and file storage identifiers from the Upload Service. The service must provide structured quotation data to the PDF Service for document generation.

**Why this priority**: Integration with external services ensures data consistency and leverages existing MALIEV microservices. This is important for operational efficiency but can be implemented incrementally as quotation features mature.

**Independent Test**: Can be fully tested by creating a quotation that requests material data from the Material Service, applies currency conversion from the Currency Service, references uploaded files from the Upload Service, and generates a PDF via the PDF Service. Delivers value by ensuring quotations contain accurate and complete information.

**Acceptance Scenarios**:

1. **Given** staff adds a line item with a material selection, **When** the material is selected, **Then** material properties and supported processes are fetched from the Material Service
2. **Given** a quotation includes pricing in multiple currencies, **When** currency conversion is needed, **Then** current rates are obtained from the Currency Service
3. **Given** a customer uploads supporting documents to the Upload Service receiving file identifiers, **When** staff creates an RFQ including these Upload Service file identifiers, **Then** the Quotation Service validates each file identifier with the Upload Service and stores FileReference entities linking the files to the RFQ
4. **Given** a quotation is ready for customer delivery, **When** staff requests a PDF, **Then** structured quotation data is sent to the PDF Service and a PDF identifier is returned
5. **Given** a quotation references materials, **When** the Material Service updates properties, **Then** the quotation reflects current material data when accessed (with caching strategy for performance)

---

### User Story 5 - Advanced Search, Analytics, and Reporting (Priority: P3)

MALIEV staff and business analysts must search, filter, and analyze RFQs and quotations to evaluate business performance. The service must support queries by customer, channel, material type, date ranges, response times, conversion rates, and other business metrics. Analytics must reveal channel performance, quotation turnaround times, abandoned requests, and pricing strategy effectiveness.

**Why this priority**: Analytics provide strategic business insights but are not essential for day-to-day quotation operations. This story delivers long-term value through data-driven decision making and can be implemented after core functionality is stable.

**Independent Test**: Can be fully tested by creating multiple RFQs and quotations across different channels and time periods, then running queries to analyze conversion rates, average turnaround time, and channel performance. Delivers value by enabling business intelligence and process improvement.

**Acceptance Scenarios**:

1. **Given** multiple RFQs exist from different channels, **When** staff analyzes conversion rates by channel, **Then** the system returns the percentage of RFQs converted to quotations for each channel
2. **Given** quotations have been created over time, **When** staff queries average turnaround time, **Then** the system calculates and displays the average time from RFQ receipt to quotation delivery
3. **Given** RFQs have various statuses, **When** staff filters for abandoned requests (RFQs not converted after 30 days), **Then** a list of abandoned RFQs is returned
4. **Given** quotations include discount structures, **When** staff analyzes pricing strategy, **Then** the system displays discount usage patterns and average discount percentages
5. **Given** staff needs to evaluate performance, **When** they generate a report by material type, **Then** the system shows quotation volume and conversion rates per material
6. **Given** business analysts need data exports, **When** they request filtered RFQ or quotation data, **Then** the system provides queryable results for further analysis

---

### Edge Cases

- What happens when an RFQ is submitted with invalid or missing customer information? System must validate required fields and reject incomplete submissions with clear error messages.
- How does the system handle concurrent updates to the same quotation by multiple staff members? System must implement optimistic locking or conflict resolution to prevent data loss.
- What happens when a staff member attempts an operation without the required role permissions? System must reject the operation and return a clear authorization error indicating insufficient permissions.
- What happens when multiple potential customer matches are suggested with similar confidence scores? System must present all potential matches to staff sorted by match confidence, allowing staff to review each and select the correct match or create a new customer record if none match.
- What happens when staff incorrectly links two customer records that should remain separate? System must provide an "unlink customers" operation that separates the records while preserving audit history of the incorrect linkage.
- What happens when an external service (Currency, Material, Upload, PDF) is unavailable? System must retry the call up to 3 times with exponential backoff, then open the circuit breaker if failures persist. While the circuit is open, the system must use cached data when appropriate and provide meaningful error messages to staff indicating degraded service.
- What happens when a circuit breaker opens for a critical service like Material Service during quotation creation? System must allow quotation creation to continue using cached material data when available, clearly indicating to staff that material properties may be stale, or gracefully fail the operation with a clear message if no cached data exists.
- What happens when a circuit breaker automatically attempts recovery and the service is still unavailable? System must keep the circuit open and continue periodic recovery attempts with appropriate backoff intervals to avoid overwhelming the failing service.
- How does the system handle RFQs with file attachments that exceed size limits or contain malicious content? System delegates file validation to the Upload Service and stores only verified file references.
- What happens when a quotation validity period expires while in draft state? System should not auto-expire drafts; expiration only applies to quotations sent to customers.
- How does the system handle quotations with materials that are discontinued or no longer available? System must flag outdated materials and prompt staff to update line items with current alternatives.
- What happens when an RFQ arrives from a new channel not yet configured in the system? System must support dynamic channel registration or provide a "generic" channel type to prevent data loss.
- How does the system handle quotation revisions after customer acceptance? System must prevent modifications to accepted quotations unless explicit "re-negotiation" workflow is initiated.
- What happens when currency conversion rates change between quotation creation and customer acceptance? System should record rates at the time of quotation creation to maintain pricing consistency.
- What happens to RFQ and quotation data that reaches the 7-year retention threshold? System must maintain data accessibility throughout the retention period and may archive or purge data after 7 years according to organizational policy and legal requirements.
- What happens when external service calls fail or timeout during a traced request? System must capture the failure in distributed traces with error details, retry up to 3 times with exponential backoff, fallback to cached data when appropriate if all retries fail, and emit metrics indicating service degradation for monitoring alerts. If the failure threshold is reached, the circuit breaker opens and subsequent calls fail immediately without retry until recovery.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST accept RFQs from multiple channels including website, LINE, WhatsApp, Facebook Messenger, Instagram, email, and in-store walk-ins
- **FR-002**: System MUST store each RFQ with channel source, customer identification, timestamp, status, and references to uploaded files
- **FR-003**: System MUST allow staff to view, search, and filter RFQs by customer, channel, date range, and status
- **FR-004**: System MUST allow staff to add internal notes to RFQs with timestamp and user identity
- **FR-005**: System MUST allow staff to assign RFQs to specific team members
- **FR-006**: System MUST allow staff to update RFQ status through defined workflow states
- **FR-007**: System MUST create quotations either from an existing RFQ or independently without an RFQ
- **FR-008**: System MUST link quotations to source RFQs when created from an RFQ
- **FR-009**: System MUST store quotation line items including quantity, material selection, manufacturing process, and pricing
- **FR-010**: System MUST store quotation metadata including delivery expectations, validity period, discount structure, and customer information
- **FR-011**: System MUST support quotation versioning, preserving full history when revisions are made
- **FR-012**: System MUST enforce quotation state transitions including draft, customer review, expired, accepted, cancelled, and pending approval
- **FR-013**: System MUST prevent invalid quotation state transitions
- **FR-014**: System MUST obtain currency conversion rates and available currencies from the Currency Service
- **FR-015**: System MUST obtain material properties, mechanical attributes, and supported manufacturing processes from the Material Service
- **FR-016**: System MUST store file references obtained from the Upload Service, not handle file uploads directly
- **FR-017**: System MUST provide structured quotation payloads to the PDF Service for document generation
- **FR-018**: System MUST NOT perform PDF rendering; all document generation is delegated to the PDF Service
- **FR-019**: System MUST record all changes to RFQs and quotations in an audit log including user identity, timestamp, action type, and changed fields
- **FR-020**: System MUST allow staff to query audit logs for compliance and tracking purposes
- **FR-021**: System MUST support advanced search and filtering of RFQs and quotations by customer, channel, material type, date ranges, and status
- **FR-022**: System MUST calculate and expose analytics including conversion rates, average turnaround time, and channel performance
- **FR-023**: System MUST identify abandoned RFQs (not converted to quotations within a defined period)
- **FR-024**: System MUST provide reporting on pricing strategies including discount usage and patterns
- **FR-025**: System MUST support caching for high-frequency read operations while maintaining database durability for all mutations
- **FR-026**: System MUST maintain internal separation between RFQ logic and quotation logic while integrating them in a unified service
- **FR-027**: System MUST validate required customer information when creating RFQs or quotations
- **FR-028**: System MUST handle external service unavailability gracefully, using cached data when appropriate and providing meaningful error messages
- **FR-029**: System MUST implement role-based access control with distinct permissions for different staff roles (e.g., sales staff can create/edit RFQs and quotations, managers can approve quotations, analysts can view analytics and reports)
- **FR-030**: System MUST enforce permission checks for all operations and prevent unauthorized actions based on user role
- **FR-031**: System MUST retain all RFQ, quotation, and audit log data for a minimum of 7 years from creation date for compliance and historical analysis
- **FR-032**: System MUST support querying and retrieval of historical data across the full 7-year retention period
- **FR-033**: System MUST emit structured logs with contextual information including request identifiers, user identity, operation type, and timestamp for all significant operations
- **FR-034**: System MUST collect and expose key business metrics including RFQ intake rate, quotation creation rate, conversion rates, average processing times, and error rates
- **FR-035**: System MUST collect and expose key technical metrics including API response times, external service call latencies, cache hit rates, and resource utilization
- **FR-036**: System MUST implement distributed tracing for all external service calls (Currency, Material, Upload, PDF services) to enable end-to-end request tracking and troubleshooting
- **FR-037**: System MUST suggest potential customer matches when a new RFQ arrives by comparing email, phone number, and name against existing customer records
- **FR-038**: System MUST allow staff to review suggested customer matches and manually confirm or reject the linkage
- **FR-039**: System MUST allow staff to manually link customer records across channels when a match is identified
- **FR-040**: System MUST maintain a unified customer view showing all RFQs and quotations across all channels once customer records are linked
- **FR-041**: System MUST implement retry logic for failed external service calls (Currency, Material, Upload, PDF) with up to 3 retry attempts using exponential backoff
- **FR-042**: System MUST implement circuit breaker pattern for each external service that opens after a failure threshold is reached, preventing cascading failures
- **FR-043**: System MUST automatically attempt to close open circuit breakers after a recovery period to restore normal service integration
- **FR-044**: System MUST emit metrics when circuit breakers open or close to enable monitoring and alerting of external service degradation

### Key Entities

- **RFQ (Request for Quotation)**: Represents a customer request for a quotation. Key attributes include unique identifier, channel source (website, LINE, WhatsApp, Facebook Messenger, Instagram, email, in-store), customer identification, file references (from Upload Service), timestamps (creation, last update), status (new, in progress, qualified, converted, abandoned), internal notes (with user and timestamp), assigned staff member, and link to resulting quotation if converted.

- **Quotation**: Represents a formal quotation issued to a customer. Key attributes include unique identifier, optional link to source RFQ, customer information, creation timestamp, current version number, validity period, status (draft, customer review, expired, accepted, cancelled, pending approval, approved), and audit metadata.

- **Quotation Version**: Represents a specific version of a quotation. Key attributes include version number, creation timestamp, creating user, line items, total pricing, discount structure, delivery expectations, and ChangeSummary (relative to previous version).

- **Quotation Line Item**: Represents a single item in a quotation. Key attributes include line number, material identifier (from Material Service), material properties (cached from Material Service), manufacturing process, quantity, unit price, line total, and any item-specific notes.

- **Audit Log Entry**: Represents a recorded change to an RFQ or quotation. Key attributes include timestamp, user identity, entity type (RFQ or Quotation), entity identifier, action type (create, update, status change, note added, assignment), and changed fields with before/after values.

- **Internal Note**: Represents staff commentary on an RFQ or quotation. Key attributes include creation timestamp, author user identity, note content, and associated entity (RFQ or Quotation).

- **Customer Reference**: Represents customer information associated with RFQs and quotations. Key attributes include customer identifier, name, contact information (email, phone), customer-specific preferences or history, and links to related customer records from different channels (when manually merged by staff). Each customer record tracks the original channel source and maintains a merge history for audit purposes.

- **Material Reference**: Represents material data obtained from the Material Service. Key attributes include material identifier, material name, mechanical properties, supported manufacturing processes, and availability status. This data is cached for performance but sourced externally.

- **File Reference**: Represents a file uploaded by customers or staff, stored in the Upload Service. Key attributes include file identifier (from Upload Service), file name, file type, upload timestamp, and associated entity (RFQ or Quotation).

- **Discount Structure**: Represents pricing discounts applied to a quotation. Key attributes include discount type (percentage, fixed amount, volume-based), discount value, conditions for application, and reason or authorization for discount.

- **Staff Role**: Represents a user role with specific permissions for system operations. Key attributes include role identifier, role name (e.g., Sales Staff, Manager, Analyst, Administrator), and associated permissions (create RFQ, edit RFQ, create quotation, edit quotation, approve quotation, view analytics, manage users).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: MALIEV staff can capture an RFQ from any supported channel and have it searchable within the system in under 5 seconds
- **SC-002**: Staff can create a quotation with multiple line items and material selections in under 3 minutes
- **SC-003**: System maintains a complete audit trail for 100% of RFQ and quotation changes with timestamp, user, and action details
- **SC-004**: 95% of RFQ search queries return results in under 2 seconds
- **SC-005**: Staff can transition a quotation through its complete lifecycle (draft to customer review to acceptance) without data loss or errors
- **SC-006**: Quotation version history is preserved with 100% accuracy, allowing staff to view any previous version
- **SC-007**: System handles integration failures with external services (Currency, Material, Upload, PDF) gracefully, providing clear error messages and using cached data when appropriate 90% of the time
- **SC-008**: Analytics queries (conversion rates, turnaround time, channel performance) complete in under 10 seconds for datasets up to 10,000 RFQs/quotations
- **SC-009**: 90% of RFQs created from multi-channel sources (website, messaging platforms, email) are successfully converted to quotations, improving tracking and reducing lost opportunities by 30% compared to the previous split-service architecture
- **SC-010**: Staff can identify abandoned RFQs (not converted within 30 days) with a single query, reducing follow-up time by 50%
- **SC-011**: System supports concurrent access by at least 50 staff members without performance degradation
- **SC-012**: 100% of quotations generated include accurate material properties and currency conversions obtained from external services
- **SC-013**: Invalid quotation state transitions are prevented 100% of the time, ensuring workflow integrity
- **SC-014**: Staff satisfaction with the unified RFQ and quotation workflow improves by 40% compared to the previous split architecture (measured via internal survey)
- **SC-015**: The unified service reduces operational overhead by consolidating RFQ and quotation management into a single system, eliminating duplicate data entry 95% of the time
- **SC-016**: Historical data queries for RFQs and quotations remain accessible and return results within acceptable performance thresholds throughout the full 7-year retention period
- **SC-017**: Operations staff can diagnose and troubleshoot production issues using structured logs, metrics dashboards, and distributed traces without requiring direct system access 90% of the time
- **SC-018**: Customer matching suggestions identify correct matches with 85% precision (true matches confirmed by staff / total suggestions displayed with confidence ≥ 60%, measured via production telemetry over 30-day rolling window when potential match exists), reducing duplicate customer records and enabling unified customer history across channels
- **SC-019**: System recovers gracefully from transient external service failures, with 95% of operations completing successfully using retry logic, and circuit breakers preventing cascading failures during prolonged outages
