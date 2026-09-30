# Test Data Management Portal

Internal enterprise portal for authorized QA/Development teams to search
customer/merchant test records, safely update selected test data in
QA/PREPROD, and mock external dependencies (Absher, Yakeen, ELM, and other
configurable APIs).

See [`CLAUDE.md`](CLAUDE.md) for the rules this repository is built under,
and [`/docs`](docs/) for full architecture/design documentation. Start with
[`docs/01-architecture.md`](docs/01-architecture.md) and
[`docs/15-development-roadmap.md`](docs/15-development-roadmap.md).

**Status:** Phases 0-7 complete (foundation through Administration &
Hardening). Remaining gaps are genuinely org-specific — see
`docs/15-development-roadmap.md`'s Phase 7 section — not something further
autonomous work can resolve without that information.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) and npm
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for
  local PostgreSQL) — or a local PostgreSQL 16 instance if Docker isn't
  available
- `dotnet-ef` CLI: `dotnet tool install --global dotnet-ef`

## Installation

```bash
git clone <repo-url>
cd MockerPortal
cp .env.example .env
```

Edit `.env` with local values (defaults work for local Docker Compose
Postgres).

## Environment configuration

The backend reads `backend/Portal.Api/appsettings.json` +
`appsettings.{ASPNETCORE_ENVIRONMENT}.json` + environment variables. See
[`docs/12-environments.md`](docs/12-environments.md) for the full model.
Locally, `ASPNETCORE_ENVIRONMENT=Development` and all enterprise
integrations run in mock mode — no real database/API access is required to
run the portal locally.

## Database

Start PostgreSQL:

```bash
docker compose up -d postgres
```

Apply migrations:

```bash
dotnet ef database update --project backend/Portal.Infrastructure --startup-project backend/Portal.Api
```

Or set `Database:AutoMigrate=true` (already the default in
`appsettings.Development.json`) to apply pending migrations automatically on
startup — never enabled for QA/PREPROD.

## Seed data

Roles and permissions are seeded automatically on every startup (idempotent,
every environment). Four Development-only seed users are also created when
`ASPNETCORE_ENVIRONMENT=Development` — never real credentials, all share the
fixed dev password `Dev-Only-Passw0rd!` (see
`IdentitySeeder.DevSeedPassword`):

| Email | Role |
|---|---|
| `admin@portal.local` | Administrator |
| `qa@portal.local` | QA |
| `developer@portal.local` | Developer |
| `readonly@portal.local` | ReadOnly |

See `docs/04-authentication-authorization.md` for the full model.

## Run backend

```bash
cd backend/Portal.Api
dotnet run
```

API available at `http://localhost:5000` (Swagger at `/swagger` in
DEV/QA). Health check at `/api/v1/health`. `dotnet run --launch-profile https`
additionally serves `https://localhost:5001`. The Vite dev proxy targets
port 5000, so it works with either profile.

## Run frontend

```bash
cd frontend/portal-ui
npm install
npm run dev
```

App available at `http://localhost:5173`, proxying API calls to the local
backend (see `frontend/portal-ui/vite.config.ts`).

## Run tests

```bash
# backend
dotnet test backend/Portal.sln

# frontend (once configured)
npm --prefix frontend/portal-ui run test
```

## CI/CD & deployment

GitLab CI (`.gitlab-ci.yml`) builds/tests both apps on every merge request
and push to the default branch, then builds and pushes container images and
deploys to OpenShift (one namespace per environment) via the Kustomize
manifests in `deploy/openshift/`. See
[`docs/13-deployment.md`](docs/13-deployment.md) §6 for the full pipeline
and manifest layout, including the CI/CD variables a platform team must
configure per environment (never invented/committed here).

## Project structure

```
/
├── CLAUDE.md               # rules this repo is built under — read first
├── .gitlab-ci.yml          # CI: build/test → images → OpenShift deploy
├── docs/                   # architecture & design documentation
├── deploy/openshift/       # Kustomize base + DEV/QA/PREPROD overlays
├── backend/                # .NET solution (Api, Application, Domain,
│                              Infrastructure, Integrations)
├── frontend/portal-ui/     # React + TypeScript + Vite app
└── tests/                  # unit / integration / API test projects
```
