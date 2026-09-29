# CLAUDE.md — Test Data Management Portal

This file governs how Claude Code (and any engineer) works in this repository.
Read this file before making any change. If a change conflicts with this file,
stop and resolve the conflict in this file first (via a deliberate edit), not
by silently deviating in code.

Full design detail lives under [`/docs`](docs/). This file is the enforceable
summary — the rules that must never be silently broken.

---

## 1. Project Overview

**Test Data Management Portal** is an internal enterprise web application for
authorized QA and Development staff to:

- Search customer/merchant test records in QA and PREPROD environments.
- View and update a controlled set of test data fields (KYC, IVR, creation
  dates, OTP cooling periods, security locks, biometrics, merchant/B2B data).
- Trigger controlled internal operations (card activation, beneficiary
  activation) via typed API clients.
- Configure and serve **mock responses** for external dependencies (Absher,
  Yakeen, ELM, and other configurable APIs) so QA/Dev can test without
  hitting real government/partner systems.

It is **not** a production customer-facing system, and it is **not** a generic
database admin tool. Every write path is a named, permissioned, audited
command — never a generic "update any field" endpoint.

See [`docs/02-requirements.md`](docs/02-requirements.md) for full scope, and
the "Removed / Out of Scope" section below for what this project explicitly
is not.

---

## 2. Architecture

```
React (TS, Vite, MUI)  →  ASP.NET Core Web API  →  Application Layer  →  Integration Providers  →  MSSQL / External APIs
                                                  ↘  PostgreSQL (portal's own DB: users, roles, screens, mock config, audit)
```

- **Portal.Domain** — entities, value objects, enums. No dependencies on
  other layers.
- **Portal.Application** — use cases (commands/queries), abstractions
  (`ICustomerProvider`, `IKycProvider`, etc.), validation, DTOs. Depends only
  on Domain.
- **Portal.Infrastructure** — EF Core/Dapper implementations against
  PostgreSQL (portal DB) and MSSQL (enterprise DBs), ASP.NET Identity, Serilog
  wiring, caching. Implements Application abstractions.
- **Portal.Integrations** — typed HTTP clients for external/internal Web APIs
  (Card Management, Beneficiary Service, B2B, Identity Verification / mock
  engine). Implements Application abstractions.
- **Portal.Api** — controllers, middleware, auth wiring, Swagger, composition
  root (DI registration). The only project that references all others.

Dependency rule: **Domain ← Application ← {Infrastructure, Integrations} ←
Api**. Infrastructure and Integrations never reference each other directly;
they only implement Application interfaces. The Api project is the only place
allowed to know about concrete implementations (via DI registration).

Full rationale: [`docs/01-architecture.md`](docs/01-architecture.md).

---

## 3. Technology Stack

**Backend:** .NET 10, ASP.NET Core Web API, C#, EF Core (portal PostgreSQL
DB), Dapper (MSSQL enterprise integrations), ASP.NET Core Identity, JWT
bearer auth, FluentValidation, Serilog, Swagger/OpenAPI, HttpClientFactory,
Polly.

**Frontend:** React + TypeScript, Vite, React Router, TanStack Query, React
Hook Form, Zod, Material UI (MUI).

**Datastore:** PostgreSQL — the portal's own application database only.
Enterprise MSSQL databases are accessed read/write only through Infrastructure
providers, never owned by this project.

Do not introduce a different ORM, state library, or UI kit without updating
this file and documenting the decision in `/docs`.

---

## 4. Repository Structure

```
/
├── CLAUDE.md
├── README.md
├── docs/                      # architecture & design docs (source of truth)
├── backend/
│   ├── Portal.Api
│   ├── Portal.Application
│   ├── Portal.Domain
│   ├── Portal.Infrastructure
│   └── Portal.Integrations
├── frontend/
│   └── portal-ui
└── tests/
    ├── Portal.UnitTests
    ├── Portal.IntegrationTests
    └── Portal.ApiTests
```

---

## 5. Coding Standards

**C# / .NET**
- Nullable reference types enabled everywhere.
- Async all the way down for I/O; always accept and propagate
  `CancellationToken` on public async methods that do I/O.
- Commands and queries are explicit classes/records (CQRS-lite), not generic
  `UpdateEntity(Dictionary<string,object>)` style methods.
- No business logic in controllers — controllers translate HTTP ⇄
  Application calls only.
- Public APIs (Application interfaces) are documented with XML doc comments
  where the "why" isn't obvious from the name.

**TypeScript / React**
- Strict mode on. No `any` unless justified with a comment.
- Server state (API data) lives in TanStack Query, not component state or a
  global store.
- Forms use React Hook Form + Zod schemas shared between the schema
  definition and the form's TypeScript types.
- No inline hex colors / spacing values in components — use theme tokens
  from `src/theme/`.

**General**
- No dead code, no commented-out blocks, no speculative abstractions for
  features not yet built.
- Prefer explicit over clever. This is an internal tool maintained by a
  rotating QA/Dev audience — optimize for readability.

---

## 6. Security Rules (non-negotiable)

1. React **never** connects directly to any database or external API. All
   access goes through `Portal.Api`.
2. **No arbitrary SQL** from the UI or API surface — ever. All DB access is
   through named repository methods with parameterized queries.
3. **No arbitrary code execution** anywhere, including in the API Mocker —
   mock responses are static/templated data, never executed scripts.
4. No database credentials, connection strings, API secrets, or tokens are
   ever sent to or stored in the React app. The frontend receives only safe
   environment metadata (name, label, color) — see
   [`docs/12-environments.md`](docs/12-environments.md).
5. All write operations require: authentication → authorization
   (permission check) → validation → audit record. No exceptions.
6. Every mutation is audited (see §8). Audit writes are not optional and are
   not best-effort — if the audit write fails, the operation is treated as
   failed (see [`docs/10-audit.md`](docs/10-audit.md) for the transactional
   approach).
7. Secrets come from environment variables or an enterprise secret store —
   never committed, never hardcoded. `.env.example` documents required
   variables; `.env` is gitignored.
8. Error responses never leak SQL exceptions, connection strings, stack
   traces, or internal infrastructure details (see §11 and
   [`docs/03-security.md`](docs/03-security.md)).
9. Production is not a supported environment for this portal. Only DEV, QA,
   and PREPROD are configured; there is no code path to a production
   connection string.

Full model: [`docs/03-security.md`](docs/03-security.md).

---

## 7. Authentication Rules

- Local email/password auth via ASP.NET Core Identity, issuing short-lived
  JWT access tokens (+ refresh token flow).
- Password policy, hashing (Identity default PBKDF2 or better), and account
  lockout after repeated failures are mandatory.
- The authentication abstraction (`ICurrentUserService`, token issuance) is
  designed so Entra ID/AD can be added later as a second
  `IAuthenticationProvider` without changing the authorization model or
  downstream permission checks.
- Seed users exist only in local/dev seed data, are clearly marked as
  non-production, and never contain real personal credentials.

Full model: [`docs/04-authentication-authorization.md`](docs/04-authentication-authorization.md).

---

## 8. Authorization Rules

- RBAC + granular permission strings (e.g. `customer.kyc.update`), enforced
  **server-side only** via ASP.NET Core policy-based authorization. The
  frontend may hide UI based on permissions for UX, but this is never treated
  as a security boundary.
- Roles: `Administrator`, `QA`, `Developer`, `ReadOnly` (seed roles; more can
  be added via admin screens later).
- Environment-scoped permissions (e.g. `customer.kyc.update.preprod`) gate
  particularly sensitive operations (security lock removal, PREPROD writes).
- Every controller action that mutates data or reads sensitive data declares
  its required permission explicitly via `[HasPermission("...")]` — no
  action is unprotected by default (fail closed).

Full model: [`docs/04-authentication-authorization.md`](docs/04-authentication-authorization.md).

---

## 9. Database Rules

- The portal owns **one** PostgreSQL database for its own concerns (users,
  roles, permissions, screens, mock config, audit). This is never confused
  with, or used to store, real enterprise customer/merchant data.
- Enterprise MSSQL databases (Customer, Merchant, IVR, Biometric, etc.) are
  accessed exclusively through Infrastructure repository classes behind
  Application-layer interfaces (`ICustomerProvider`, `IMerchantProvider`,
  ...). Each has its own named connection string — never invented, never
  shared, never hardcoded.
- No connection string, schema, or field is invented. If real schema/field
  detail is not supplied, the provider is implemented with an explicit mock/
  in-memory adapter and the gap is documented in
  [`docs/05-database-design.md`](docs/05-database-design.md) under
  "Unknowns."
- EF Core is used for the portal's own PostgreSQL schema (migrations are
  committed). Dapper/ADO.NET is used for MSSQL enterprise integrations where
  existing stored procedures/schemas make more sense.

---

## 10. API Rules

- REST, versioned under `/api/v1/...`.
- Every write endpoint maps to exactly one named Application command (e.g.
  `UpdateKycCommand`, `RemoveSecurityLockCommand`) — no generic PATCH-any-
  field endpoints.
- All responses use the standard envelope (success/error) —
  see [`docs/06-api-design.md`](docs/06-api-design.md) and §11 below.
- Swagger/OpenAPI is kept up to date as part of Definition of Done (§14).
- External/internal service calls (Card Management, Beneficiary, B2B,
  Identity Verification) go through typed clients built on
  `HttpClientFactory` + Polly (timeout, retry, circuit breaker as
  appropriate) — never raw `HttpClient` instantiated ad hoc.

---

## 11. Dynamic UI Rules

- Screen layout/fields are driven by metadata (`Screen`, `ScreenField`,
  `ScreenAction`, `ScreenPermission`) rendered by a reusable
  `DynamicForm`/`DynamicField` React component set.
- Metadata controls **presentation and validation hints only**. The actual
  business operation behind a "Save" action is always a specific, reviewed
  backend command — metadata never dynamically determines *what SQL/API call
  happens*, only *what the form looks like and what permission gates it*.
- Adding a new field to an existing screen's metadata must never
  automatically grant a new database write — the backend command's allowed
  field set is defined in code, independent of the UI metadata.

Full design: [`docs/07-dynamic-screen-engine.md`](docs/07-dynamic-screen-engine.md).

---

## 12. API Mock Rules

- The mocker matches on HTTP method + path + header/query/body rules, and
  returns a **configured static/templated response** (status, headers, body,
  delay). It never executes arbitrary code, scripts, or SQL to produce a
  response.
- Mock configuration changes are permissioned (`api-mocker.manage`,
  `api-mocker.enable`, environment-scoped variants) and audited like any
  other mutation.
- New mock-able APIs are added purely through configuration
  (`MockApi`/`MockEndpoint`/`MockResponse`/`MockMatchRule` rows) — the mock
  engine itself is never modified to special-case a specific API.

Full design: [`docs/09-api-mocker.md`](docs/09-api-mocker.md).

---

## 13. Audit Requirements

- Every mutation (KYC update, security lock removal, card activation, mock
  config change, etc.) writes an `AuditLogs` entry: who, when, environment,
  target entity, field-level old/new values (masked where sensitive),
  operation result, correlation ID.
- Audit writes never include passwords, tokens, or API secrets. PII is
  masked per the rules in [`docs/10-audit.md`](docs/10-audit.md).
- Audit is part of the write pipeline (§ "Write Operation Pattern" in the
  master spec), not a fire-and-forget side effect.

---

## 14. Environment Rules

- Supported environments: `DEV`, `QA`, `PREPROD`. No production connection
  path exists in this codebase.
- Per-environment configuration lives in `appsettings.{Environment}.json` +
  environment variables for secrets — never hardcoded URLs or credentials.
- The current environment is always visible in the UI header and is
  impossible to miss (distinct color/badge per environment).
- Some permissions are environment-scoped (`*.preprod` suffix) and enforced
  server-side regardless of what the UI shows.

Full model: [`docs/12-environments.md`](docs/12-environments.md).

---

## 15. Testing Rules

- New Application-layer commands/queries ship with unit tests (happy path +
  at least one authorization/validation failure path).
- New integration providers ship with tests against a test double or
  testcontainer, not against real enterprise systems.
- New API endpoints ship with at least one API test covering auth failure,
  permission failure, and success.
- `dotnet build` and `dotnet test` must both pass before a phase is
  considered complete. Frontend: `npm run build` and `npm run test` (once
  configured) must pass.

Full strategy: [`docs/14-testing-strategy.md`](docs/14-testing-strategy.md).

---

## 16. Git Rules

- Conventional commits: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`,
  `chore:`, `security:`.
- Focused commits — one logical change per commit.
- Never commit `.env`, secrets, credentials, database dumps, or environment-
  specific production-like configuration. `.env.example` documents required
  variables instead.
- Never use `--no-verify`, force-push, or amend commits unless the user
  explicitly asks.

---

## 17. Development Workflow

1. Read this file and the relevant `/docs` file(s) before starting.
2. Work in phases as defined in
   [`docs/15-development-roadmap.md`](docs/15-development-roadmap.md). Do not
   jump ahead to later-phase screens before earlier phases build and pass
   tests.
3. When an integration detail (schema, field, API contract) is unknown,
   implement behind an interface with a mock/local adapter, and record the
   gap in the relevant doc's "Unknowns" section — do not invent enterprise
   details.
4. After each meaningful change: backend builds (`dotnet build`), frontend
   builds (`npm run build`), relevant tests pass.
5. Update Swagger and the relevant `/docs` file whenever behavior changes.

---

## 18. Definition of Done

A feature/phase is not complete unless:

- [ ] Backend builds with no errors/warnings-as-errors violations.
- [ ] Frontend builds (`npm run build`) with no type errors.
- [ ] Relevant tests exist and pass.
- [ ] Authorization is implemented and enforced server-side.
- [ ] Validation is implemented (FluentValidation / Zod).
- [ ] Audit is implemented for every mutation.
- [ ] Error handling follows the standard envelope; no internal detail leaks.
- [ ] Swagger reflects the new/changed endpoints.
- [ ] Relevant `/docs` file is updated.
- [ ] No secrets committed; new required config is documented in
      `.env.example`.
- [ ] UI follows the STC Bank-inspired theme (uses theme tokens, no
      hardcoded colors).
- [ ] Feature works end-to-end against local mocks/test doubles (no
      dependency on real enterprise systems to demo).

---

## 19. Explicitly Out of Scope / Removed

- **Test Scenarios module** — removed, do not implement.
- **Generic workflow/scenario engine** — not part of this project.
- Any direct production database or production API access.
- Arbitrary SQL execution, arbitrary code execution, or a generic "run this
  script" facility anywhere in the product, including the API Mocker.
- Over-engineering: do not build plugin systems, multi-tenancy, or other
  speculative infrastructure not called for by the current phase.
