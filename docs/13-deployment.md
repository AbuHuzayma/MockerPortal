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
iteration — local dev never needs the container images described in §3;
those exist for DEV/QA/PREPROD deployment.

## 2. Required local tooling

- .NET 10 SDK
- Node.js 20+ / npm
- Docker Desktop (for PostgreSQL via Compose) — or a locally installed
  PostgreSQL 16 if Docker is unavailable
- `dotnet-ef` CLI tool (`dotnet tool install --global dotnet-ef`) for
  migrations

## 3. Containerization (Phase 7)

`Portal.Api` and `portal-ui` are each containerized (`backend/Portal.Api/
Dockerfile`, `frontend/portal-ui/Dockerfile`) now that Phase 7 confirmed an
actual deployment target (OpenShift — §4). Local dev is unaffected: `dotnet
run`/`npm run dev` are still the fast inner loop; the containers exist for
DEV/QA/PREPROD deployment and for anyone who wants to run the full stack
locally via `docker-compose.yml`.

Both images run as an arbitrary non-root UID (no `USER` pinned, no ports
below 1024) so they work unmodified under OpenShift's default `restricted`
SCC — no extra `oc adm policy add-scc-to-user` grant needed.

The frontend image bakes no environment-specific value in at build time.
`docker-entrypoint.d/env-config.sh` regenerates `env-config.js` from the
`VITE_API_BASE_URL` env var at container *start*, which
`frontend/portal-ui/src/api/client.ts` reads before falling back to the
Vite build-time value. This is what lets **one** built image be promoted
DEV → QA → PREPROD unchanged, matching §6's pipeline.

## 4. QA / PREPROD deployment — confirmed platform

**Repo:** GitLab (this file's copy on GitHub is a mirror — see the project's
actual GitLab remote for the canonical CI-connected copy).
**CI:** GitLab CI (`.gitlab-ci.yml` at the repo root).
**Hosting:** OpenShift — one namespace/project per environment (DEV, QA,
PREPROD), deployed via the Kustomize manifests in `deploy/openshift/`.

Still genuinely unknown, and deliberately **not** guessed at — every one of
these is a required GitLab CI/CD variable or a `CHANGE_ME` placeholder in
`deploy/openshift/base/*-configmap.yaml`, filled in by the platform team,
never invented here:

- The actual OpenShift cluster API URLs, ServiceAccount tokens, and
  namespace names per environment (`OPENSHIFT_SERVER_*`,
  `OPENSHIFT_TOKEN_*`, `OPENSHIFT_NAMESPACE_*` — §6).
- The cluster's apps domain, i.e. what hostname the auto-generated
  `portal-api`/`portal-ui` Routes actually get (`oc get route` after first
  apply — §6).
- Enterprise secret store integration (`deploy/openshift/base/
  backend-secret.example.yaml` documents the required keys; the real
  `Secret` is provisioned per namespace out-of-band — plain `oc create
  secret`, a sealed-secret, or a vault integration, whichever the platform
  team uses — never committed here regardless).
- Log aggregation destination (Serilog currently writes to console only,
  which OpenShift already collects as pod logs by default; a
  cluster-wide/ELK sink is an infra decision to document here once made).

## 5. Build validation (CI)

Every phase must satisfy, and `.gitlab-ci.yml`'s `test` stage runs exactly
this on every merge request and every push to the default branch:

```
dotnet build backend/Portal.sln
dotnet test  backend/Portal.sln
npm --prefix frontend/portal-ui run lint
npm --prefix frontend/portal-ui run build
```

(No `npm run test` yet — no frontend test runner is configured. Add it to
both this list and `.gitlab-ci.yml`'s `frontend:build` job together, not
separately, so they can never drift apart.)

## 6. CI/CD pipeline and OpenShift manifests

```
.gitlab-ci.yml
  test            dotnet build/test (backend:test), npm lint/build (frontend:build)
                  — every MR and every push to the default branch
  build-images    docker build + push to $CI_REGISTRY_IMAGE/{backend,frontend}
                  — default branch only, after :test passes
  deploy          oc + kustomize apply per environment
                  — deploy:dev automatic; deploy:qa and deploy:preprod are
                    `when: manual` (deploy:preprod additionally `needs`
                    deploy:qa, so PREPROD can never be promoted ahead of QA)

deploy/openshift/
  base/                       shared Deployment/Service/Route/ConfigMap for
                               portal-api and portal-ui
  base/backend-secret.example.yaml
                               documents required Secret keys — never
                               applied, never committed with real values
  overlays/{dev,qa,preprod}/  ASPNETCORE_ENVIRONMENT per environment (must
                               match a backend/Portal.Api/appsettings.*.json
                               file exactly — case-sensitive on Linux:
                               Development / QA / Preprod), replica count
                               (preprod: 2, since it's the only environment
                               with real external integrations — see
                               appsettings.Preprod.json)
```

**One image pair, promoted unchanged.** `backend:build-image`/
`frontend:build-image` tag images with `$CI_COMMIT_SHORT_SHA` (plus
`:latest`); every `deploy:*` job re-tags that *same* SHA into its overlay
via `kustomize edit set image` — DEV, QA, and PREPROD always run identical
bits, differing only in the ConfigMap-driven `ASPNETCORE_ENVIRONMENT`
(which appsettings.{Env}.json is loaded) and whatever's in each
namespace's real `Secret`.

**First deploy to a new namespace** needs one manual bootstrap step, because
Route hostnames aren't known until OpenShift generates them: apply once,
`oc get route portal-api portal-ui`, then fill in the real hostnames in
`backend-configmap.yaml`'s `Cors__AllowedOrigins__0` and
`frontend-configmap.yaml`'s `VITE_API_BASE_URL` (both currently
`CHANGE_ME` placeholders), and re-apply. After that, ordinary deploys never
touch this again.

**Local validation** (no cluster needed, catches Kustomize/YAML mistakes
before they reach CI):

```
kubectl kustomize deploy/openshift/overlays/dev
kubectl kustomize deploy/openshift/overlays/qa
kubectl kustomize deploy/openshift/overlays/preprod
```

## 7. Health checks

`GET /api/v1/health` (ASP.NET Core Health Checks) reports:

- API process liveness.
- PostgreSQL connectivity.
- (As real providers are added) a lightweight connectivity check per active
  enterprise integration, without leaking connection details in the
  response — status only (`Healthy`/`Degraded`/`Unhealthy`) per dependency
  name.
