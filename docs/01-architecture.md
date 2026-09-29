# 01 — Architecture

## 1. Goals

- Keep all access to enterprise customer/merchant data and external APIs
  behind the .NET backend — React never talks to a database or external API
  directly.
- Make every write operation an explicit, reviewable command — never a
  generic "update this table" endpoint.
- Allow real MSSQL schemas, external API contracts, and business rules to be
  plugged in later without an architectural rewrite (abstractions first,
  concrete integrations second).
- Support DEV/QA/PREPROD as first-class, backend-enforced environments.

## 2. Style: Layered / Clean Architecture (lite)

```
┌─────────────────────────────────────────────────────────────────┐
│ Portal.Api                                                       │
│  Controllers, middleware, auth wiring, Swagger, DI composition   │
└───────────────┬───────────────────────────────────┬─────────────┘
                 │                                   │
     ┌───────────▼───────────┐           ┌───────────▼─────────────┐
     │ Portal.Infrastructure  │           │ Portal.Integrations     │
     │ EF Core (Postgres),    │           │ Typed HTTP clients:     │
     │ Dapper (MSSQL),        │           │ CardManagementClient,   │
     │ Identity, Serilog      │           │ BeneficiaryServiceClient│
     │ sinks, caching         │           │ B2BServiceClient,       │
     │                        │           │ IdentityVerificationClient
     └───────────┬────────────┘           └───────────┬─────────────┘
                 │           implements Application interfaces
                 └───────────────────┬───────────────────┘
                                      │
                        ┌─────────────▼─────────────┐
                        │ Portal.Application         │
                        │ Commands, Queries, DTOs,   │
                        │ Validators, Interfaces      │
                        │ (ICustomerProvider, etc.)  │
                        └─────────────┬───────────────┘
                                      │
                        ┌─────────────▼─────────────┐
                        │ Portal.Domain               │
                        │ Entities, enums, value       │
                        │ objects. No dependencies.    │
                        └───────────────────────────────┘
```

Chosen over a "vertical slice"/feature-folder style because this project has
many external systems with genuinely different persistence technologies
(Postgres via EF Core, several MSSQL DBs via Dapper, several HTTP APIs) — a
strict ports-and-adapters boundary keeps that heterogeneity from leaking into
Application/Domain code, and keeps the "abstraction first, real
integration later" requirement mechanical rather than a matter of discipline.

## 3. Dependency rule

`Portal.Domain` ← `Portal.Application` ← {`Portal.Infrastructure`,
`Portal.Integrations`} ← `Portal.Api`.

- Domain has zero project references.
- Application references Domain only.
- Infrastructure and Integrations reference Application (to implement its
  interfaces) and Domain. They never reference each other.
- Api references all four, and is the only project allowed to new-up
  concrete Infrastructure/Integrations types (via DI registration in
  `Program.cs` / extension methods).

This is enforced by project references, not just convention — Infrastructure
and Integrations projects physically cannot see each other's internals
because neither references the other.

## 4. Request flow (write operation)

```
HTTP request
  → AuthN middleware (JWT)
  → AuthZ middleware (permission policy on the endpoint)
  → Controller (thin: validates the request via FluentValidation, calls an
    Application service directly — no mediator, see §6)
  → Application service method
      → loads current state via a Provider interface (ICustomerProvider, ...)
      → applies the change via the same/another Provider interface
      → returns a Result<T>
  → Audit service writes an AuditLog row (same logical transaction where the
    target store supports it; compensating log entry otherwise — see
    docs/10-audit.md)
  → Controller maps Result<T> → standard API envelope
```

No controller ever builds SQL, calls `DbContext`/`SqlConnection` directly, or
calls an external HTTP client directly — that always happens inside an
Infrastructure/Integrations class behind an Application interface.

## 5. Request flow (read operation)

Reads follow the same shape minus the audit write (reads of sensitive data
—e.g. KYC view— may still be audited per `docs/10-audit.md` if flagged
sensitive), and typically go through a `Query`/`QueryHandler` pair rather
than a `Command`/`CommandHandler` pair, kept in the same Application project
but a distinct folder (`Application/Queries` vs `Application/Commands`).

## 6. Mediation

**Decided in Phase 1, confirmed in Phase 2: no mediator library.** Controllers
call a small Application-layer service interface directly (`IAuthService`,
`ICustomerService`, ...), injected via constructor DI. Each service method
returns `Result<T>` (`Portal.Application.Common.Result`) so expected failures
(not found, invalid credentials) don't require exceptions or a mediator
pipeline to translate into an HTTP response — the controller maps `Result<T>`
to the standard envelope itself. This was chosen over MediatR because the
service-per-feature-area shape (one interface per bounded concern: auth,
customers, later merchants/mocker/audit) gives the same controller/
Application decoupling without an extra abstraction layer or package,
consistent with CLAUDE.md's "don't over-engineer" rule. Revisit only if a
genuine cross-cutting pipeline need appears (e.g. generic validation/logging
behaviors across many handlers) that direct service calls can't express
cleanly.

## 7. Multiple databases

The portal touches two categories of data store:

1. **Portal's own PostgreSQL database** — users, roles, permissions,
   screens/fields metadata, mock configuration, audit log. Owned and
   migrated by this project via EF Core.
2. **Enterprise MSSQL databases** (Customer, Merchant, IVR, Biometric, ...)
   — pre-existing, owned by other systems. Accessed read/write only through
   named provider classes in `Portal.Infrastructure` using Dapper/ADO.NET,
   each with its own connection string (see `docs/05-database-design.md`).
   The portal never runs migrations against these databases.

## 8. External/internal Web APIs

Card Management, Beneficiary Service, B2B Subscription Matrix, and identity
verification (Absher/Yakeen/ELM) are accessed via typed clients in
`Portal.Integrations`, built on `HttpClientFactory` + Polly. Each client
implements an Application-layer interface (e.g. `ICardManagementClient`) so
Application code never depends on `HttpClient` directly, and so a client can
be swapped for the API Mocker or a test double without changing callers.

## 9. Environment separation

Environment (`DEV`/`QA`/`PREPROD`) is resolved server-side from
configuration/deployment (never from a client-supplied header) and flows
through the request as an `IEnvironmentContext`. Providers and clients use it
to select the correct connection string / base URL. See
`docs/12-environments.md`.

## 10. Unknowns at this stage

- Whether an in-process mediator library (MediatR) or a hand-rolled
  dispatcher is used — decide and document at start of Phase 1.
- Exact enterprise MSSQL schemas/connection topology beyond what the master
  spec enumerates — tracked per-provider in `docs/05-database-design.md`.
- Whether an existing enterprise API Gateway exists for mock routing — see
  `docs/09-api-mocker.md` §"Routing topology".
