# 12 — Environment Model

## 1. Supported environments

`DEV`, `QA`, `PREPROD`. There is no production connection path in this
codebase — the master spec scopes this tool to internal QA/Dev use against
non-production systems only.

## 2. Per-environment configuration

```
backend/Portal.Api/
  appsettings.json                -- shared defaults, no secrets, no real URLs
  appsettings.Development.json     -- DEV overrides (points at mocks/local)
  appsettings.QA.json                -- QA overrides (connection string NAMES + non-secret URLs)
  appsettings.Preprod.json             -- PREPROD overrides
```

- Files contain **non-secret** configuration: which provider mode
  (`Sql`/`Mock`) is active, feature flags, base URL *hostnames* where not
  considered sensitive, logging levels.
- **Secrets** (connection string credentials, API keys, JWT signing key)
  come from environment variables (local dev / Docker Compose `.env`,
  gitignored) or an enterprise secret store in QA/PREPROD deployments —
  never committed to any `appsettings.*.json`.
- ASP.NET Core's standard configuration precedence applies:
  `appsettings.json` → `appsettings.{Environment}.json` → environment
  variables → (future) secret store provider. Later sources override
  earlier ones, so secrets injected via env vars always win.

## 3. Environment resolution

- Backend: `ASPNETCORE_ENVIRONMENT` selects the config file; an explicit
  `Environment:Name` config value (`DEV`/`QA`/`PREPROD`) is what the app
  logic actually reads (kept distinct from ASP.NET's own `Development`/
  `Staging`/`Production` names to avoid conflating hosting environment with
  this app's DEV/QA/PREPROD business concept).
- This resolved value is exposed via `IEnvironmentContext` (server-side) and
  returned to the frontend only as safe metadata:

```json
{
  "name": "QA",
  "label": "QA",
  "color": "#0288D1"
}
```

  via `GET /api/v1/auth/me` (and a lightweight unauthenticated
  `/api/v1/meta/environment` for the login page banner) — never a
  connection string or internal hostname.
- The frontend never lets a user "switch" environment client-side; each
  deployment of the frontend is built/configured to talk to one backend,
  which serves one environment. Switching environments means using the
  QA-deployed portal vs. the PREPROD-deployed portal (or, locally, choosing
  which `appsettings` config runs).

## 4. Per-environment connection strings (named, not invented)

Only connection strings actually required and supplied are configured. The
naming convention:

```
ConnectionStrings:PortalDb            -- PostgreSQL, this project's own DB
ConnectionStrings:CustomerDatabase       -- MSSQL, enterprise
ConnectionStrings:MerchantDatabase         -- MSSQL, enterprise
ConnectionStrings:IvrDatabase                 -- MSSQL, enterprise (when supplied)
ConnectionStrings:BiometricDatabase             -- MSSQL, enterprise (when supplied)
```

Each is environment-specific (a QA `CustomerDatabase` string differs from a
PREPROD one) and is only present in configuration for the environments
where it's actually needed/available. Local DEV uses `Providers:*:Mode =
Mock` and typically has none of the MSSQL connection strings set at all.

## 5. Environment-scoped permissions

Some operations are gated by an environment-suffixed permission in addition
to the base permission (see `docs/04-authentication-authorization.md` §2):
`customer.kyc.update.qa`, `customer.kyc.update.preprod`,
`customer.security.remove.preprod`, `api-mocker.enable.preprod`. Enforcement
compares the endpoint's declared environment-scoped permission against
`IEnvironmentContext.Name` server-side — a QA-only-permissioned user gets
`403 FORBIDDEN` if they somehow call a PREPROD-deployed instance.

## 6. Local development environment

Local DEV runs with:

- PostgreSQL via Docker Compose (portal's own DB).
- All enterprise MSSQL/external-API providers in `Mock` mode.
- A relaxed (but still real) JWT signing key from `.env` (gitignored,
  documented in `.env.example`).

This lets the entire stack — auth, dynamic screens, a sample customer flow,
the API Mocker — run and be demoed without any access to real enterprise
systems, satisfying the master spec's "runnable without production systems"
requirement.

## 7. Unknowns

- Actual QA/PREPROD hosting details (on-prem VM, Kubernetes, IIS, etc.) —
  see `docs/13-deployment.md` "Unknowns".
- Which enterprise secret store product (Azure Key Vault, HashiCorp Vault,
  etc.) the organization uses, if any, beyond environment variables.
