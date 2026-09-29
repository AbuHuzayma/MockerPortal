# 15 — Development Roadmap

Phases are sequential; a phase is not "started" in earnest until the
previous phase satisfies CLAUDE.md §18 Definition of Done. Each phase
result should build, and be committed, before moving to the next.

## Phase 0 — Foundation ✅ complete

- Repository structure, `.gitignore`, `.editorconfig`.
- .NET solution (`Portal.sln`) with the 5 backend projects wired per
  `docs/01-architecture.md`'s dependency rule, plus 3 test projects.
- React + Vite + TypeScript app scaffold with MUI, React Router, TanStack
  Query, React Hook Form, Zod installed; theme skeleton per
  `docs/11-ui-design.md`.
- PostgreSQL via Docker Compose.
- Serilog logging, global exception handling middleware, standard response
  envelope, Swagger, health check endpoint.
- Environment configuration skeleton (`appsettings.*.json`, `.env.example`).
- App shell in React: header, sidebar nav (all sections from
  `docs/11-ui-design.md` §4, unauthenticated placeholder pages), environment
  badge.
- CI-equivalent build validation: `dotnet build` and `npm run build` both
  succeed.

**No business integrations, no auth, no real screens yet.**

## Phase 1 — Authentication & Authorization ✅ complete

- ASP.NET Core Identity wired to PostgreSQL.
- `/auth/login`, `/auth/refresh`, `/auth/me`, `/auth/logout`.
- JWT issuance + refresh token storage/rotation.
- Roles/Permissions schema + seed data (`docs/04-authentication-authorization.md` §2).
- Permission-policy authorization infrastructure (`[HasPermission(...)]`).
- React: login page, auth context/hooks, protected route wrapper,
  permission-aware nav filtering.
- Dev-only seed users (never real credentials).
- API tests for the full auth flow.

## Phase 2 — Customer Foundation ✅ complete

- `ICustomerProvider` abstraction, `MockCustomerProvider` (seeded fixtures,
  default everywhere) and `SqlCustomerProvider` (Dapper against
  `T_PRT_CUSTOMER`, selected via `Providers:Customer:Mode=Sql`) both
  implemented from the start, behind the same interface —
  `ICustomerService`/`CustomersController` never know which is active.
- `GET /customers/search?mobileNumber=...` + `GET /customers/{customerId}`,
  customer context (`CustomerContextValue` — master spec §9's exact shape)
  held client-side in `CustomerContext`/`useCustomerContext`, in-memory only
  (cleared on logout, not persisted across a reload — a reload re-searches).
- Read-only customer profile view (`CustomerProfilePage`) covering the
  identity/status subset of the master spec's KYC field list; the full KYC
  field set is edited by the dedicated screen in Phase 4.
- Error handling: `CUSTOMER_NOT_FOUND` (404) and `VALIDATION_FAILED` (400),
  both rendered inline on the search page.
- Audit foundation: `AuditLogs` table + `IAuditService`/`AuditService`
  implemented and covered by integration tests proving the write path works
  end-to-end; no product code writes a real entry yet (no mutations exist
  before Phase 4) — masking (docs/10-audit.md §4) is deferred to when there's
  a real sensitive field to mask.

## Phase 3 — Dynamic Screen Framework ✅ complete

- `Screens`/`ScreenFields`/`ScreenActions`/`ScreenPermissions` schema (plus
  an `OptionsJson` addition for Select/MultiSelect — see docs/05 §2) +
  `GET /api/v1/screens/{code}`, permission-filtered (docs/07 §2).
- React `DynamicForm`/`DynamicField` covering all 12 control types
  (`docs/07-dynamic-screen-engine.md` §4-5), including the confirmation
  dialog for actions that require one.
- One working sample screen (`SAMPLE_SCREEN`, `/dev/sample-screen`) proving
  the full round trip end-to-end, including the write-operation pattern and
  an audit write per changed field — see docs/07 §7.

## Phase 4 — Customer Screens ✅ complete

1. KYC ✅ complete — `IKycProvider`/`MockKycProvider`/`SqlKycProvider`,
   `KycFieldCatalog`-driven screen metadata (all ~68 fields + computed PEP),
   whitelisted updates, per-changed-field audit with mobile-number masking,
   full test coverage (unit/integration/API), `KycPage` wired to
   `DynamicForm`. Surfaced and fixed two Phase 1 permission gaps along the
   way — see docs/04 §2.
2. IVR ✅ complete — deliberately zero-field: `IIvrProvider`/`MockIvrProvider`
   (no `SqlIvrProvider` exists, and "Sql" mode fails fast rather than
   guessing), `IvrFieldCatalog` is an empty, documented whitelist per the
   master spec's explicit "do not invent field names" instruction (§11). The
   screen, endpoints, and audit plumbing are fully wired; `IvrPage` shows an
   honest "pending schema" message instead of a form. Ready to receive real
   fields the moment schema is supplied — see docs/05 §3.
3. Customer Creation ✅ complete — `ICreationProvider`/`MockCreationProvider`/
   `SqlCreationProvider`, `CreationFieldCatalog` (3 fields: createdDate,
   dateOfBirth, kycDate — two with an ambiguous column name in the spec
   itself, see the catalog's doc comment), date-of-birth-not-in-future
   validation.
4. OTP / IVR Cooling ✅ complete — same zero-field pattern as IVR
   (`OtpCoolingFieldCatalog` empty; no field names given in the spec).
5. Card ✅ complete — `ICardManagementClient` (ASSUMED CONTRACT, docs/08 §4),
   `MockCardManagementClient` (in-memory, None→Created→Active),
   `HttpCardManagementClient` in Portal.Integrations for "Real" mode. "Real"
   mode's DI registration lives in `Portal.Api/Program.cs`, not
   `Portal.Infrastructure` — Infrastructure must never reference
   Integrations (docs/01 §5).
6. Beneficiary ✅ complete — `IBeneficiaryServiceClient` (ASSUMED CONTRACT),
   same Mock/Http split as Cards. No "list beneficiaries" data exists yet,
   so the UI operates on one fixed mock beneficiary ID (`BEN-0001`).
7. Security Lock removal ✅ complete — a typed `SecurityLockStatus` record
   (unlike KYC/IVR's bag, since these 4 fields are concretely named and
   distinctly typed), `ISecurityLockProvider` with both Mock and Sql
   implementations, gated by `customer.security.remove` (environment-scoped
   — PREPROD in practice, since no `.qa` variant is ever granted).
8. Biometrics ✅ complete — same zero-field pattern as IVR (docs/05 §3 marks
   BiometricDatabase entirely unknown).
9. Onboarding ✅ complete — read-only, composed from `ICustomerService` +
   `ICreationService` (no new provider — every field it needs already
   exists elsewhere).

Real external API integrations (Card, Beneficiary, Absher/Yakeen) are not
implemented until their contracts are supplied — each screen ships against
its provider/client's mock implementation, fully functional end-to-end, with
the real implementation swapped in later behind the same interface.

## Phase 5 — Merchant ✅ complete

- Merchant search (by name, partial match — no search key was specified in
  the spec), `MerchantContext` (mirrors `CustomerContext`), merchant details
  (`NAME_EN`, `NAME_AR`, `BRAND_NAME_EN`, `BRAND_NAME_AR`, `CR_EXPIRY_DATE`,
  `ID_EXPIRY_DATE` via `MerchantFieldCatalog`), `IMerchantProvider` with both
  Mock and Sql implementations.
- B2B integration via `IB2BServiceClient` (ASSUMED CONTRACT, docs/08 §4) —
  same Mock/Http split as Cards/Beneficiary, "Real" mode wired in
  `Portal.Api/Program.cs`.

## Phase 6 — API Mocker ✅ complete

- `MockApis`/`MockEndpoints`/`MockResponses`/`MockMatchRule` schema
  (`Portal.Infrastructure/Mocking`), `IMockEngine`/`MockEngine` (matching +
  `{{token}}` templating, closed-grammar only — docs/09 §3-4), scoped to the
  environment the portal is deployed as (same scoping `IMockAdminService`
  uses).
- `MockAdminController` (`/api/v1/mock-admin/apis`) — get/list/upsert (whole
  tree)/delete/enable/disable, gated by `api-mocker.view` /
  `api-mocker.manage` / `api-mocker.enable` (environment-scoped) /
  `api-mocker.disable`; every write audited (`AuditLogs`, screen
  `API_MOCKER`).
- `MockServingController` (`/mock/{apiCode}/{**path}`, any HTTP method) —
  intentionally anonymous per docs/09 §7 (called by systems under test, not
  portal users); every call logged to `OperationLogs` regardless of match
  outcome.
- Admin UI (`ApiMockerPage` + `MockApiEditorDialog`) for Absher/Yakeen/ELM/
  Other APIs — list, enable/disable, and a nested endpoint→response→
  match-rule editor.
- `MockDataSeeder` — starter Absher/Yakeen/ELM scenarios (illustrative field
  names only, no real contract supplied — docs/08 §4), seeded disabled so
  nothing serves mock traffic until explicitly enabled.
- Found and fixed during implementation: `MockAdminService` originally
  looked up a `MockApi` by `Code` alone, which threw once the same code
  existed in more than one environment (exactly what `MockDataSeeder`
  creates); all admin operations are now scoped by the deployed environment,
  matching `MockEngine`'s existing scoping, and the environment on
  create/update is always the server's own, never client-supplied.

## Phase 7 — Administration & Hardening ✅ complete (except org-specific unknowns)

- `AdminUsersController` (`/api/v1/admin/users`, `admin.users`) — list/
  create/set-role/enable/disable/reset-password, backed by
  `IAdminUserService`/`AdminUserService` (ASP.NET Core Identity
  `UserManager`/`RoleManager` directly — no new abstraction needed since
  Identity already is one). A user can't disable their own account
  (`CANNOT_DISABLE_SELF`). Every write audited (screen `ADMIN`).
- `AdminUsersController` also exposes `/admin/roles` (`admin.roles`) and
  `/admin/permissions` (`admin.permissions`) — both read-only: roles and the
  permission catalog are defined in `PermissionCatalog`/`IdentitySeeder`
  (code, reviewed), never admin-editable at runtime — docs/07 §6.
- `AdminScreensController` (`/admin/screens`, `admin.screens`) — read-only
  raw view of every seeded `Screen`/fields/actions, via a new
  `IScreenService.ListAllAsync` that (unlike `GetScreenDefinitionAsync`)
  is intentionally unfiltered by caller permission, since it's an admin
  review tool, not the normal screen-rendering path.
- `AdminIntegrationsController` (`/admin/integrations`, `admin.integrations`)
  — read-only status view of every `Providers:{name}:Mode` from
  `IConfiguration` plus the current environment. Mode itself stays a
  deployment-time config decision, never runtime-editable here.
- `AuditController` (`/api/v1/audit`, `audit.view`) + `IAuditQueryService`/
  `AuditQueryService` — paginated, filterable query over `AuditLogs`
  (date range, username, customerId, merchantId, screen, entity, result).
- Frontend: `AdminPage` (tabbed: Users/Roles & Permissions/Screens/
  Integrations, each tab hidden unless the user holds its permission) and
  `AuditPage` (filter bar + paginated table).
- Security hardening: fixed-window rate limiting (20 req/min per client IP)
  on `/auth/login` and `/auth/refresh` (`Microsoft.AspNetCore.RateLimiting`,
  no new package), and `SecurityHeadersMiddleware` adding
  `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`,
  `Permissions-Policy` to every response. No CSP — this is a JSON API, not
  server-rendered HTML.
- `OperationLogs` (docs/10 §6) implemented — `IOperationLogService`/
  `OperationLogService`, written by `MockServingController` for every mock
  call (Phase 6).
- **Deferred — genuinely org-specific, not resolvable from the spec alone**:
  CI pipeline (needs the org's CI platform confirmed) and the remaining
  `docs/13-deployment.md` unknowns (real DB/secret-store endpoints, actual
  external API base URLs). Documented as gaps, not guessed at — consistent
  with the master spec's "don't invent" rule.

## Explicit non-goals (every phase)

- Test Scenarios module — not built.
- Generic workflow/scenario engine — not built.
- Production access — never added.
- Arbitrary SQL/code execution anywhere — never added.
