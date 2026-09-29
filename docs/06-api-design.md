# 06 — API Design

## 1. Conventions

- Base path: `/api/v1/...`. Breaking changes bump to `/api/v2` rather than
  mutating v1 in place.
- JSON everywhere, camelCase property names.
- All timestamps UTC, ISO-8601.
- All endpoints (except `/auth/login`, `/auth/refresh`, `/health`) require a
  valid JWT bearer token.
- All mutating endpoints declare a required permission (see
  `docs/04-authentication-authorization.md`).

## 2. Response envelope

**Success:**

```json
{
  "success": true,
  "data": { "...": "..." },
  "correlationId": "c7b1..."
}
```

**Error:**

```json
{
  "success": false,
  "error": {
    "code": "CUSTOMER_NOT_FOUND",
    "message": "Customer was not found.",
    "details": null
  },
  "correlationId": "c7b1..."
}
```

- `code` is a stable machine-readable string the frontend can switch on.
- `message` is safe to show a user as-is.
- `details` (optional) carries structured validation errors
  (`{ "field": "email", "message": "..." }[]`) for 400 responses only.
- `correlationId` is always present (generated per-request if not supplied
  via an inbound `X-Correlation-Id` header) and is echoed in audit logs and
  Serilog output, so a user can report "correlation ID X failed" and it's
  traceable end-to-end.

## 3. HTTP status mapping

| Situation | Status | `error.code` example |
|---|---|---|
| Validation failure | 400 | `VALIDATION_FAILED` |
| Not authenticated | 401 | `UNAUTHENTICATED` |
| Authenticated, lacks permission | 403 | `FORBIDDEN` |
| Target entity not found | 404 | `CUSTOMER_NOT_FOUND` / `MERCHANT_NOT_FOUND` |
| Conflict (e.g. concurrent update) | 409 | `CONCURRENCY_CONFLICT` |
| Unexpected server error | 500 | `INTERNAL_ERROR` |
| Downstream integration failure | 502 | `INTEGRATION_UNAVAILABLE` |

## 4. Endpoint groups (Phase-aligned; grows over the roadmap)

```
Auth
  POST   /api/v1/auth/login
  POST   /api/v1/auth/refresh
  POST   /api/v1/auth/logout
  GET    /api/v1/auth/me

Customers
  GET    /api/v1/customers/search?mobileNumber=...
  GET    /api/v1/customers/{customerId}
  GET    /api/v1/customers/{customerId}/kyc
  PUT    /api/v1/customers/{customerId}/kyc
  GET    /api/v1/customers/{customerId}/ivr
  PUT    /api/v1/customers/{customerId}/ivr
  GET    /api/v1/customers/{customerId}/creation
  PUT    /api/v1/customers/{customerId}/creation
  GET    /api/v1/customers/{customerId}/otp-cooling
  PUT    /api/v1/customers/{customerId}/otp-cooling
  GET    /api/v1/customers/{customerId}/cards
  POST   /api/v1/customers/{customerId}/cards/activate
  GET    /api/v1/customers/{customerId}/beneficiaries/{beneficiaryId}
  POST   /api/v1/customers/{customerId}/beneficiaries/{beneficiaryId}/activate
  GET    /api/v1/customers/{customerId}/security
  POST   /api/v1/customers/{customerId}/security/remove-lock
  GET    /api/v1/customers/{customerId}/biometrics
  PUT    /api/v1/customers/{customerId}/biometrics
  GET    /api/v1/customers/{customerId}/onboarding

Merchants
  GET    /api/v1/merchants/search?...
  GET    /api/v1/merchants/{merchantId}
  PUT    /api/v1/merchants/{merchantId}
  POST   /api/v1/merchants/{merchantId}/b2b

Screens (dynamic UI metadata)
  GET    /api/v1/screens/{code}

API Mocker
  GET    /api/v1/mock-admin/apis
  POST   /api/v1/mock-admin/apis
  GET    /api/v1/mock-admin/apis/{id}/endpoints
  POST   /api/v1/mock-admin/apis/{id}/endpoints
  POST   /api/v1/mock-admin/endpoints/{id}/responses
  PUT    /api/v1/mock-admin/responses/{id}
  POST   /api/v1/mock-admin/responses/{id}/match-rules
  {*}    /mock/{apiCode}/{**path}      -- the actual mock-serving route, see docs/09-api-mocker.md

Audit
  GET    /api/v1/audit?filters...

Admin
  GET/POST/PUT  /api/v1/admin/users
  GET/POST/PUT  /api/v1/admin/roles
  GET/POST/PUT  /api/v1/admin/permissions
  GET/POST/PUT  /api/v1/admin/screens
  GET/POST/PUT  /api/v1/admin/integrations

Health
  GET    /api/v1/health
```

Each write endpoint maps to exactly one Application command (see
`docs/01-architecture.md` §4 and CLAUDE.md §10) — there is no
`PATCH /customers/{id}` generic endpoint.

## 5. Pagination & filtering (list endpoints)

```
GET /api/v1/audit?page=1&pageSize=25&userId=...&fromDate=...&toDate=...
```

```json
{
  "success": true,
  "data": {
    "items": [ "..." ],
    "page": 1,
    "pageSize": 25,
    "totalCount": 134
  },
  "correlationId": "..."
}
```

## 6. Concurrency

Update endpoints accept an optional `rowVersion`/`etag`-equivalent from the
last read where the underlying provider supports optimistic concurrency;
conflicts return `409 CONCURRENCY_CONFLICT` rather than silently
overwriting. Where the underlying enterprise system has no such concept,
last-write-wins is documented explicitly per provider.

## 7. Swagger/OpenAPI

Generated via `Swashbuckle`, served at `/swagger` in DEV/QA (disabled or
auth-gated in PREPROD per `docs/12-environments.md`). JWT bearer auth is
wired into the Swagger UI for interactive testing. Kept current as part of
Definition of Done for every endpoint change.
