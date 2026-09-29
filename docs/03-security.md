# 03 — Security Model

## 1. Threat model summary

This portal handles sensitive test data that may resemble real customer PII
(KYC, national ID references, biometric status) and holds the ability to
mutate that data and trigger internal financial-adjacent operations (card
activation, beneficiary activation). The primary risks addressed:

- Unauthorized read/write of customer/merchant data by an authenticated but
  under-privileged user.
- Privilege escalation via a trusted-frontend assumption (frontend-only
  permission checks).
- Injection (SQL, mock-response templating) leading to data corruption or
  information disclosure.
- Credential/secret leakage via the frontend bundle, logs, or error
  responses.
- Cross-environment contamination (e.g. a QA credential being usable to
  write PREPROD data it shouldn't touch).

## 2. Hard rules (see also CLAUDE.md §6)

1. React never holds a DB connection string, MSSQL/Postgres credential, or
   external API secret. It receives a JWT access token and safe environment
   metadata only.
2. No SQL is ever built from unvalidated, unparameterized user input.
   Dapper queries use parameters; EF Core uses LINQ. No string-concatenated
   SQL, anywhere, ever.
3. The API Mocker returns **data**, not **code**: response bodies are
   static text/JSON with optional simple token substitution (e.g.
   `{{request.body.mobileNumber}}`) evaluated by a fixed, non-Turing-complete
   template engine — never `eval`, never a scripting sandbox, never
   arbitrary C#/JS execution.
4. Every controller action requires authentication by default
   (`[Authorize]` globally, `[AllowAnonymous]` only on
   `/auth/login`/health checks, and — a second, deliberate exception —
   the API Mocker's `/mock/{apiCode}/{**path}` serving route, per
   docs/09-api-mocker.md §7: it's called by systems under test, not portal
   users, and is restricted at the network/environment level instead).
   Every mutating/sensitive action additionally requires an explicit
   permission policy — there is no "authenticated users can do X" default
   for anything beyond basic navigation.
5. Authorization decisions are never trusted from the client. The frontend
   may hide buttons based on `GET /auth/me` permissions for UX, but the
   backend re-checks on every request.
6. Secrets (DB passwords, JWT signing key, external API keys) load from
   environment variables / an enterprise secret store at runtime. They are
   never committed, never embedded in `appsettings.{Env}.json` values
   directly (those files reference variable names/placeholders only).
7. Rate limiting is applied to `/auth/login` (and other unauthenticated
   endpoints) to slow credential stuffing/brute force.
8. Input validation happens server-side (FluentValidation) regardless of
   client-side (Zod) validation — the client-side check is UX only.

## 3. Data classification

| Class | Examples | Handling |
|---|---|---|
| Secret | DB passwords, JWT signing key, external API keys | Env vars/secret store only; never logged |
| Sensitive PII | National ID, KYC fields, biometric status, mobile number | Masked in logs; full value shown in UI only to permitted users; audited on view where flagged |
| Internal | Screen metadata, mock config | Normal access control, no special masking |
| Public | Environment name/color, app version | Safe to send to frontend unauthenticated (login page needs environment banner) |

## 4. Error handling

All API errors use the standard envelope (`docs/06-api-design.md`). The
global exception handler middleware:

- Catches all unhandled exceptions.
- Logs full detail (including stack trace) server-side via Serilog with a
  correlation ID.
- Returns to the client only: an error `code`, a safe `message`, and the
  `correlationId` — never the exception message/stack trace/connection
  string for unexpected (5xx-class) errors.
- Known/expected failures (validation, not-found, forbidden) return a
  specific `code` and a user-safe `message` written by the developer, not
  derived from an exception.

## 5. Transport & session

- HTTPS enforced (HSTS in QA/PREPROD-hosted deployments).
- JWT access tokens are short-lived (default 15 minutes, configurable);
  refresh tokens are longer-lived, stored server-side (or as an httpOnly
  cookie, decided in Phase 1), and revocable.
- CORS restricted to the known frontend origin(s) per environment.

## 6. Auditability as a security control

Because this tool can mutate what looks like real customer data, every
mutation is audited (see `docs/10-audit.md`) specifically so that misuse is
detectable after the fact, not just prevented up front. Audit logs are
themselves access-controlled (`audit.view` permission) and are never
editable through the API (append-only).

## 7. Dependency & supply chain

- No destructive or mass-scale tooling is introduced.
- Backend/frontend dependencies are added deliberately; avoid adding a
  package for something that's a few lines of code, to keep the audit
  surface small.

## 8. What this document intentionally does not cover yet

Real penetration-test findings, WAF/network-layer controls, and enterprise
secret-store product choice are organization decisions outside this
repository's control — to be documented here once known
(`docs/13-deployment.md` "Unknowns").
