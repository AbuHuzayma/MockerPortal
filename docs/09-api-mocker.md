# 09 — API Mocker

## 1. Purpose

Let QA/Dev configure canned responses for external dependencies (Absher,
Yakeen, ELM, and other configurable APIs) so integration testing doesn't
require real connectivity to government/partner systems, while guaranteeing
the mocker can never become a code-execution or SQL-execution surface.

## 2. Data model

See `docs/05-database-design.md` §2: `MockApis → MockEndpoints →
MockResponses → MockMatchRule`. Summary:

- **MockApi** — a logical external API (`ABSHER`, `YAKEEN`, `ELM`, ...),
  scoped to an environment, enable/disable at this level.
- **MockEndpoint** — one path + HTTP method under a `MockApi`.
- **MockResponse** — one candidate response for an endpoint: status code,
  headers, body, delay, `Priority`, enable/disable.
- **MockMatchRule** — zero or more conditions that must all match (per
  `MockResponse`) against the incoming request's header/query string/path/
  body, for that response to be selected. A `MockResponse` with no rules is
  the default/fallback for its endpoint.

Selection: for an incoming request, find the active `MockEndpoint` matching
path+method, evaluate its `MockResponse`s in `Priority` order, return the
first one whose `MockMatchRule`s all pass (or the ruleless default if none
specifically match). If nothing matches and no default exists, return `404`
with a clear "no mock configured" body — never silently fall through to a
real call unless mock mode is explicitly configured to do so.

## 3. Matching

| Source | Example |
|---|---|
| Header | `X-Test-Case: NATIONAL_ID_NOT_FOUND` |
| QueryString | `?nationalId=1234567890` |
| Path | path segment equals/contains a value |
| RequestBody | JSON path or substring match against the raw body |

Operators (v1): `Equals`, `NotEquals`, `Contains`, `StartsWith`. No regex/
scripting operator in v1 — if a case genuinely needs more, it's a deliberate
future addition to this doc and the operator enum, not an escape hatch to
arbitrary matching code.

## 4. Response templating (explicitly NOT code execution)

`ResponseBody` supports simple `{{token}}` substitution from a fixed,
whitelisted set of sources evaluated by a non-Turing-complete templating
step:

```
{{request.header.X-Correlation-Id}}
{{request.query.nationalId}}
{{request.body.<jsonPath>}}
{{now}}                       -- current UTC timestamp
{{uuid}}                      -- generated correlation-style id
```

This is string substitution against a closed token grammar — **no**
expression evaluation, **no** arbitrary JavaScript/C#, **no** access to
anything outside the current request's header/query/body and a couple of
fixed helpers. This boundary is enforced in code review and is a hard rule
from CLAUDE.md §6/§12 — it must never grow into a scripting sandbox.

## 5. Serving route

```
{ANY} /mock/{apiCode}/{**path}
```

A single generic controller/middleware resolves `apiCode` → `MockApi`,
matches `path` + HTTP method → `MockEndpoint`, evaluates responses, and
returns the configured status/headers/body after the configured
`DelayMilliseconds`. Adding a new mock-able API is purely a data change
(insert `MockApi`/`MockEndpoint`/`MockResponse` rows via the admin UI/API)
— the serving middleware is never modified per-API.

## 6. Routing topology (client → mock vs. real)

```
Client Application (or this portal's own IIdentityVerificationClient)
       │
       ▼
Configured base URL for the dependency
       │
       ├── if pointed at this portal's /mock/{apiCode}/... route → Mock Engine → MockResponse
       │
       └── if pointed at the real external base URL → Real API
```

Which base URL a given client/environment uses is **configuration**
(`Providers:IdentityVerification:Mode` = `Mock` | `Real`, or per-environment
base URL), not a runtime decision made by the mock engine itself. This keeps
the mock engine a passive responder rather than a routing decision-maker
over real traffic.

If the organization has an existing enterprise API Gateway that other
applications call through, the recommended integration is: the gateway
routes calls for a mocked dependency to this portal's `/mock/{apiCode}/...`
endpoint when that dependency's mock is enabled, and to the real endpoint
otherwise — configured at the gateway, not assumed or hardcoded here. **No
specific gateway product is assumed** until confirmed; this portal exposes a
stable, documented mock-serving contract either way.

## 7. Authorization & audit

- Managing mock config (`api-mocker.manage`) and enabling/disabling mocks
  (`api-mocker.enable` / `api-mocker.disable`, environment-scoped variants
  e.g. `api-mocker.enable.preprod`) are permissioned separately from viewing
  (`api-mocker.view`).
- Every create/update/enable/disable of `MockApi`/`MockEndpoint`/
  `MockResponse`/`MockMatchRule` writes an `AuditLogs` entry.
- The `/mock/{apiCode}/...` serving route itself is typically called by
  *other* systems under test, not by an authenticated portal user — it is
  therefore **not** behind the portal's JWT auth, but is restricted at the
  network/environment level (only reachable from QA/PREPROD test traffic)
  and is fully logged (`OperationLogs`) for traceability.

## 8. Extensibility

Adding Absher/Yakeen/ELM (Phase 6) and any future API is done by:

1. Inserting a `MockApi` row (Code, Name, Environment).
2. Inserting `MockEndpoint` rows for its paths/methods.
3. Inserting `MockResponse` + `MockMatchRule` rows for the scenarios QA
   needs.

No change to the mock engine's controller, matching logic, or data model is
required — if one is ever needed, it's a deliberate, documented schema
change, not a per-API special case.

## 9. Implementation status (Phase 6)

Shipped as designed above, with one clarification learned during
implementation: every admin operation (`/api/v1/mock-admin/apis/...`) and
the serving route (`/mock/{apiCode}/...`) is scoped to the environment this
portal instance is deployed as (`IEnvironmentContext`) — a `MockApi.Code` is
only unique within that scope, not globally. A shared dev/test database can
still hold the same code across DEV/QA/PREPROD rows (as the Absher/Yakeen/
ELM starter seed does); each environment's deployment only ever sees and
manages its own rows. `MockServingController` writes one `OperationLogs`
row per call (§7), whether or not a response was found.
