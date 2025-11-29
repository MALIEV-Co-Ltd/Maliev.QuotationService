# API Contract Overview: Quotation Service

**Version**: v1
**Base Path**: `/quotation/v1`
**Documentation**: `/quotation/scalar/v1` (Development only)
**OpenAPI Spec**: `/quotation/openapi/v1.json`

## Authentication

All endpoints (except health checks) require JWT Bearer token authentication.

**Authorization Header**:
```
Authorization: Bearer <JWT_TOKEN>
```

## API Endpoint Groups

### 1. RFQ Management (`/quotation/v1/rfqs`)

| Method | Endpoint | Authorization | Description |
|--------|----------|---------------|-------------|
| POST | `/rfqs` | EmployeeOrHigher | Create new RFQ from any channel |
| GET | `/rfqs` | EmployeeOrHigher | List RFQs with filtering (channel, status, date range) |
| GET | `/rfqs/{id}` | EmployeeOrHigher | Get RFQ details |
| PUT | `/rfqs/{id}` | EmployeeOrHigher | Update RFQ details |
| PATCH | `/rfqs/{id}/status` | EmployeeOrHigher | Update RFQ status |
| POST | `/rfqs/{id}/notes` | EmployeeOrHigher | Add internal note to RFQ |
| PATCH | `/rfqs/{id}/assign` | Employee | Assign RFQ to staff member |
| POST | `/rfqs/{id}/convert` | EmployeeOrHigher | Convert RFQ to quotation |

### 2. Quotation Management (`/quotation/v1/quotations`)

| Method | Endpoint | Authorization | Description |
|--------|----------|---------------|-------------|
| POST | `/quotations` | EmployeeOrHigher | Create new quotation (from RFQ or standalone) |
| GET | `/quotations` | EmployeeOrHigher | List quotations with filtering (status, customer, date range) |
| GET | `/quotations/{id}` | EmployeeOrHigher | Get quotation details with current version |
| GET | `/quotations/{id}/versions` | EmployeeOrHigher | Get all versions of a quotation |
| GET | `/quotations/{id}/versions/{versionNumber}` | EmployeeOrHigher | Get specific quotation version |
| PUT | `/quotations/{id}` | EmployeeOrHigher | Update quotation (creates new version) |
| PATCH | `/quotations/{id}/status` | EmployeeOrHigher | Update quotation status |
| POST | `/quotations/{id}/approve` | Manager | Approve quotation (requires Manager role) |
| POST | `/quotations/{id}/notes` | EmployeeOrHigher | Add internal note to quotation |
| GET | `/quotations/{id}/pdf` | EmployeeOrHigher | Generate PDF (delegates to PDF Service) |

### 3. Customer Management (`/quotation/v1/customers`)

| Method | Endpoint | Authorization | Description |
|--------|----------|---------------|-------------|
| GET | `/customers/{id}` | EmployeeOrHigher | Get customer details with unified RFQ/quotation history |
| POST | `/customers/match` | Employee | Get customer match suggestions for RFQ |
| POST | `/customers/link` | Employee | Manually link customers across channels |
| POST | `/customers/unlink` | Employee | Unlink incorrectly merged customers |

### 4. Analytics & Reporting (`/quotation/v1/analytics`)

| Method | Endpoint | Authorization | Description |
|--------|----------|---------------|-------------|
| GET | `/analytics/conversion-rates` | EmployeeOrHigher | Get RFQ-to-quotation conversion rates by channel |
| GET | `/analytics/turnaround-time` | EmployeeOrHigher | Get average turnaround time metrics |
| GET | `/analytics/abandoned-rfqs` | EmployeeOrHigher | List abandoned RFQs (not converted within threshold) |
| GET | `/analytics/pricing-strategy` | EmployeeOrHigher | Analyze discount usage and patterns |
| GET | `/analytics/channel-performance` | EmployeeOrHigher | Get performance metrics by channel |

### 5. Health & Observability

| Method | Endpoint | Authorization | Description |
|--------|----------|---------------|-------------|
| GET | `/quotation/liveness` | None | Liveness probe (service is running) |
| GET | `/quotation/readiness` | None | Readiness probe (service is ready to accept traffic) |
| GET | `/quotation/metrics` | None | Prometheus metrics endpoint |

## Common Response Patterns

### Success Responses

```json
// Single Resource (200 OK)
{
  "data": {
    "id": "uuid",
    "...": "resource fields"
  }
}

// List Resources (200 OK)
{
  "data": [ /* array of resources */ ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 150,
    "totalPages": 8
  }
}

// Created (201 Created)
{
  "data": {
    "id": "uuid",
    "...": "resource fields"
  },
  "location": "/quotation/v1/rfqs/uuid"
}
```

### Error Responses

```json
// Validation Error (400 Bad Request)
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "Email": ["Email address is required"],
    "PhoneNumber": ["Phone number must be in E.164 format"]
  }
}

// Unauthorized (401 Unauthorized)
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "JWT token is missing or invalid"
}

// Forbidden (403 Forbidden)
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "User does not have permission to perform this action. Required role: Manager"
}

// Not Found (404 Not Found)
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "RFQ with ID '12345' was not found"
}

// Conflict (409 Conflict)
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Conflict",
  "status": 409,
  "detail": "Quotation has been modified by another user. Please refresh and try again."
}

// Service Unavailable (503 Service Unavailable)
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.4",
  "title": "Service Unavailable",
  "status": 503,
  "detail": "Material Service is currently unavailable. Using cached data where possible."
}
```

## Rate Limiting

- **Global**: 100 requests/minute per user
- **Batch Operations** (analytics endpoints): 10 requests/minute
- **Response Headers**:
  - `X-RateLimit-Limit`: Maximum requests allowed in window
  - `X-RateLimit-Remaining`: Requests remaining in current window
  - `X-RateLimit-Reset`: Timestamp when window resets

## Pagination

List endpoints support pagination via query parameters:

```
GET /quotation/v1/rfqs?page=2&pageSize=50
```

- `page`: Page number (1-indexed, default: 1)
- `pageSize`: Items per page (default: 20, max: 100)

## Filtering & Sorting

Common query parameters for list endpoints:

**RFQs**:
- `channel`: Filter by channel (Website, LINE, WhatsApp, etc.)
- `status`: Filter by status (New, InProgress, Qualified, Converted, Abandoned)
- `customerId`: Filter by customer ID
- `assignedStaffUserId`: Filter by assigned staff
- `fromDate`, `toDate`: Date range filter
- `sortBy`: Sort field (createdAt, updatedAt)
- `sortOrder`: asc or desc

**Quotations**:
- `status`: Filter by status (Draft, PendingApproval, Approved, CustomerReview, Accepted, Expired, Cancelled)
- `customerId`: Filter by customer ID
- `fromDate`, `toDate`: Date range filter
- `validityExpiring`: Boolean (only show quotations expiring soon)
- `sortBy`: Sort field (createdAt, validityPeriodEnd, totalPrice)
- `sortOrder`: asc or desc

## External Service Integration Headers

The service integrates with external MALIEV services. Calls include:

- `X-Correlation-Id`: Request correlation ID for distributed tracing
- `X-User-Id`: Current user ID (propagated from JWT)
- `X-Service-Name`: "Maliev.QuotationService"
- `X-Request-Timestamp`: ISO 8601 timestamp

## Circuit Breaker Behavior

When external services are unavailable:

1. **Retry**: 3 attempts with exponential backoff (2s, 4s, 8s)
2. **Circuit Open**: After failure threshold, circuit opens for 30 seconds
3. **Fallback**: Use cached data where applicable (Material, Currency)
4. **Response**: 503 Service Unavailable with clear error message indicating degraded service

## Detailed Endpoint Specifications

Full OpenAPI specifications for each endpoint are available in separate contract files:
- `rfq-endpoints.md`: RFQ Management API details
- `quotation-endpoints.md`: Quotation Management API details
- `customer-endpoints.md`: Customer Management API details
- `analytics-endpoints.md`: Analytics & Reporting API details

**OpenAPI Schema**: `/quotation/openapi/v1.json` (machine-readable)
**Interactive Docs**: `/quotation/scalar/v1` (Development environment only)

## Versioning

API versioning via URL path segment: `/quotation/v{version}/resource`

- **Current Version**: v1
- **Backward Compatibility**: Guaranteed within major version
- **Breaking Changes**: New major version (v2) with migration guide

## CORS Configuration

Allowed origins (configured via Google Secret Manager):
- `https://intranet.maliev.com` (Internal staff portal)
- `https://api.maliev.com` (API gateway)

Allowed methods: GET, POST, PUT, PATCH, DELETE, OPTIONS
Allowed headers: Authorization, Content-Type, X-Correlation-Id
