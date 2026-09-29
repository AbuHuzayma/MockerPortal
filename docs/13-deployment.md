# 13 — Deployment & Local Development Strategy

## 1. Local development (defined now)

Docker Compose provides the one piece of infrastructure this project
actually owns: PostgreSQL.

```
docker-compose.yml
  postgres:
    image: postgres:16
    environment: POSTGRES_DB / POSTGRES_USER / POSTGRES_PASSWORD (from .env)
    ports: 5432:5432
    volumes: named volume for data persistence
```

Backend (`dotnet run` from `backend/Portal.Api`) and frontend (`npm run dev`
from `frontend/portal-ui`) run natively on the developer's machine for fast
iteration — they are not containerized in v1 (see §3 for why, and when that
changes).

## 2. Required local tooling

- .NET 10 SDK
- Node.js 20+ / npm
- Docker Desktop (for PostgreSQL via Compose) — or a locally installed
  PostgreSQL 16 if Docker is unavailable
- `dotnet-ef` CLI tool (`dotnet tool install --global dotnet-ef`) for
  migrations

## 3. Why not containerize the API/UI yet

Phase 0 optimizes for the fastest inner dev loop for a small team building
incrementally; the API and UI have no external OS-level dependencies that
justify containerization yet. Dockerizing `Portal.Api` and `portal-ui` is a
straightforward addition (each gets a `Dockerfile`, added to
`docker-compose.yml`) once there's an actual QA/PREPROD deployment target
that requires container images — tracked as a Phase 7 hardening item, not
implemented speculatively now (CLAUDE.md §20: don't over-engineer the first
version).

## 4. QA / PREPROD deployment — unknowns

The organization's actual hosting model for QA/PREPROD (IIS, Kubernetes,
Azure App Service, on-prem VM, existing CI/CD pipeline, reverse
proxy/gateway product) is **not yet known** and is not guessed at here. This
document will be updated with:

- Target hosting platform per environment.
- CI/CD pipeline definition (build → test → publish → deploy).
- TLS/certificate handling.
- Reverse proxy / API Gateway configuration (also relevant to
  `docs/09-api-mocker.md` §6 routing topology).
- Enterprise secret store integration (replacing plain environment
  variables for QA/PREPROD secrets).
- Log aggregation destination (currently Serilog writes to console + rolling
  file locally; a QA/PREPROD sink — e.g. Seq, ELK, Application Insights — is
  an organizational decision to be documented here once known).

## 5. Build validation (CI)

Even before a full CI/CD pipeline exists, every phase must satisfy:

```
dotnet build backend/Portal.sln
dotnet test  backend/Portal.sln          # once tests exist
npm --prefix frontend/portal-ui run build
npm --prefix frontend/portal-ui run test  # once configured
```

A minimal CI workflow (e.g. GitHub Actions) running these four commands on
push/PR is a reasonable Phase 0/7 addition once the org's CI platform is
confirmed — not assumed here without confirmation of which CI system the
org uses.

## 6. Health checks

`GET /api/v1/health` (ASP.NET Core Health Checks) reports:

- API process liveness.
- PostgreSQL connectivity.
- (As real providers are added) a lightweight connectivity check per active
  enterprise integration, without leaking connection details in the
  response — status only (`Healthy`/`Degraded`/`Unhealthy`) per dependency
  name.
