# 14 — Testing Strategy

## 1. Test projects

```
tests/
  Portal.UnitTests           -- Application layer: handlers, validators,
                                 permission logic, mock matching, mapping
  Portal.IntegrationTests      -- Infrastructure: EF Core against a real/
                                   testcontainer Postgres, MSSQL providers
                                   against test doubles, API clients against
                                   a mock HTTP server (WireMock.Net or similar)
  Portal.ApiTests                -- Full API via WebApplicationFactory:
                                     auth, authorization, end-to-end command
                                     flows against mock providers
```

Frameworks: xUnit, FluentAssertions, Moq (or NSubstitute) for Application
unit tests; `Testcontainers` for Postgres in integration tests where
practical (falls back to a local Postgres instance if Docker isn't
available in CI); `WebApplicationFactory<Program>` for API tests.

## 2. Unit tests (Portal.UnitTests)

Cover, per CLAUDE.md §15, for every new command/query:

- Happy path (valid input → expected result, expected provider/client calls
  made with correct arguments).
- At least one validation failure path (FluentValidation rejects invalid
  input, handler never calls a provider).
- At least one authorization-adjacent path where relevant (e.g. environment-
  scoped permission logic itself, tested independently of ASP.NET's
  authorization pipeline).
- Mock-matching logic (`docs/09-api-mocker.md` §3): each operator
  (`Equals`, `NotEquals`, `Contains`, `StartsWith`) across Header/
  QueryString/Path/RequestBody sources, priority ordering, default/fallback
  response selection.
- Audit diff computation and masking rules (`docs/10-audit.md` §4).

## 3. Integration tests (Portal.IntegrationTests)

- EF Core repositories against a real Postgres (testcontainer) —
  migrations apply cleanly, CRUD round-trips correctly, concurrency tokens
  work.
- MSSQL-backed providers tested against **test doubles** (in-memory fakes
  implementing the same interface, or a local test schema if/when real
  schema is supplied) — never against real enterprise QA/PREPROD databases
  from automated tests.
- Typed API clients (`ICardManagementClient`, etc.) tested against a mock
  HTTP server verifying request shape, retry/timeout/circuit-breaker
  behavior (Polly policies actually fire under simulated failures).

## 4. API tests (Portal.ApiTests)

For every endpoint group added in a phase:

- 401 when unauthenticated.
- 403 when authenticated but lacking the required permission (including
  environment-scoped permission cases).
- 200/2xx happy path with expected envelope shape.
- 400 with structured `details` on validation failure.
- Audit row is written on successful mutation (asserted via the test's own
  Postgres instance).

Minimum required before Phase 1 is "done": auth (login/refresh/me/logout)
fully covered this way.

## 5. Frontend tests

Using Vitest + React Testing Library (added when the frontend scaffold is
in place):

- Login form (validation, error display, redirect on success).
- Protected route redirect when unauthenticated / lacking permission.
- Customer search (loading/empty/error/result states).
- `DynamicForm` rendering from a sample screen metadata fixture, including
  permission-based field hiding.
- Confirmation dialog shows correct current/new values before submit.
- API error envelope renders a user-safe message, not raw error detail.

## 6. What is explicitly not tested with real systems

No automated test — unit, integration, or API — ever runs against a real
enterprise MSSQL database, a real Card Management/Beneficiary/B2B API, or a
real Absher/Yakeen/ELM endpoint. All of those are test doubles or the
portal's own API Mocker in test runs, consistent with CLAUDE.md's "runnable
without production systems" rule and avoiding any risk of tests mutating
real test-environment data.

## 7. Definition of Done gate

Per CLAUDE.md §18 and §15: `dotnet build` + `dotnet test` (backend) and
`npm run build` (+ `npm run test` once configured) must pass before a phase
is considered complete. This is a manual gate in early phases; automated in
CI once `docs/13-deployment.md` §5's CI workflow exists.
