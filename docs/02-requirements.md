# 02 — Requirements

## 1. Purpose

Internal QA/Development tool to search customer/merchant test records and
safely update selected test data in QA/PREPROD, plus an API Mocker for
external dependencies.

## 2. Users

- **QA** — searches customers/merchants, updates test data within their
  permissions, uses the API Mocker to simulate external dependencies during
  test execution.
- **Developer** — similar to QA, typically with broader mock-management
  permissions for debugging integration issues.
- **Administrator** — manages users/roles/permissions/screens/integrations,
  full access across environments the org allows.
- **ReadOnly** — search and view only, no mutations.

## 3. In-scope capabilities

| # | Capability | Environment(s) | Category |
|---|---|---|---|
| 1 | Search customers by mobile number (+ future identifiers) | QA/PREPROD | Customer |
| 2 | View customer information | QA/PREPROD | Customer |
| 3 | Update KYC details | QA/PREPROD | Customer |
| 4 | Update IVR details | QA/PREPROD | Customer |
| 5 | Update customer creation dates | QA/PREPROD | Customer |
| 6 | Update OTP/IVR cooling period | QA/PREPROD | Customer |
| 7 | Activate/create cards (via internal API) | QA/PREPROD | Customer |
| 8 | Activate internal transfer beneficiaries (via internal API) | QA/PREPROD | Customer |
| 9 | Remove customer security locks | PREPROD | Customer |
| 10 | Update biometric expiry | QA/PREPROD | Customer |
| 11 | View onboarding information (read-only) | QA/PREPROD | Customer |
| 12 | Update merchant information | QA/PREPROD | Merchant |
| 13 | Add merchant to B2B services | QA/PREPROD | Merchant |
| 14 | Mock external APIs (Absher, Yakeen, ELM, configurable others) | configurable | API Mocker |
| 15 | Audit trail of all mutations | all | Cross-cutting |
| 16 | User/role/permission/screen/integration administration | all | Admin |

Absher/Yakeen verification fields (`YAKEEN_STATUS_ID`, `YAKEEN_CHECK_DATE`,
`YAKEEN_NEXT_DATE`) are scoped to **PREPROD** per the master spec.

## 4. Out of scope (explicitly removed)

- **Test Scenarios module** — removed.
- **Generic workflow/scenario engine** — not built.
- Any production data access.
- Update operations on the Onboarding screen (read-only unless explicitly
  added later).
- Arbitrary SQL/code execution anywhere, including in the mocker.

## 5. Non-functional requirements

- **Auditability**: every mutation traceable to a user, timestamp,
  environment, and before/after field values.
- **Least privilege**: server-enforced RBAC + granular + environment-scoped
  permissions.
- **Safety**: destructive/sensitive actions (security lock removal) require
  explicit UI confirmation showing exactly what will change.
- **Resilience**: external API calls use timeouts, retries (Polly), and
  never hang the request indefinitely.
- **Portability of environment config**: no hardcoded URLs/credentials;
  DEV/QA/PREPROD configured independently.
- **Extensibility without core changes**: new mock-able APIs, new screens,
  and new integration providers are addable through configuration/new
  classes implementing existing interfaces — not by modifying the mock
  engine or core write pipeline.
- **Local runnability**: the whole stack must run locally against Postgres +
  mock/test-double providers, without access to real enterprise systems.

## 6. Data sensitivity

Customer KYC data, national IDs, biometric status, and merchant registration
data are treated as sensitive even in QA/PREPROD (they may be
production-like/synthetic-but-realistic test data). Handling rules are in
`docs/03-security.md` and `docs/10-audit.md` (masking).

## 7. Known unknowns (tracked, not guessed)

- Real MSSQL schema/column types for IVR, Biometric, and parts of the
  Merchant DB beyond the fields explicitly listed in the master spec.
- Exact contract (request/response shape) for Absher/Yakeen/ELM real
  integrations.
- Exact contract for Card Management, Beneficiary Service, and B2B
  Subscription Matrix APIs.
- Whether an enterprise API Gateway already exists for mock routing.
- Whether/when Entra ID/AD integration will be required.

Each is implemented behind an interface with a documented mock/local adapter
until the real detail is supplied (see `docs/08-integration-architecture.md`).
