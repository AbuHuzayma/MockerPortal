# 10 — Audit

## 1. What gets audited

- Every mutation of enterprise data (KYC, IVR, creation dates, OTP cooling,
  security lock removal, biometric expiry, merchant/B2B updates).
- Every controlled API-triggered operation (card activation, beneficiary
  activation).
- Every API Mocker configuration change (create/update/enable/disable of
  `MockApi`/`MockEndpoint`/`MockResponse`/`MockMatchRule`).
- Every admin change (user/role/permission/screen/integration management).
- Login/logout and failed login attempts (for security monitoring), recorded
  distinctly from business-data audit rows (via `OperationLogs`/Identity's
  own tracking) rather than mixed into `AuditLogs`.

Reads are **not** audited by default, except viewing of specifically
flagged-sensitive screens (KYC, security lock status) may optionally be
audited as a `View` operation if the organization requires it — configurable
per screen (`ScreenActions` can include a `VIEW` action with
`RequiresAudit: true`), off by default in v1 to avoid audit-log noise, and
can be enabled per screen without a schema change.

## 2. Audit record shape

See `docs/05-database-design.md` §2 `AuditLogs`:

```
Id, UserId, Username, Environment, CustomerId, MerchantId, Screen,
Operation, Entity, Field, OldValue, NewValue, Result, ErrorMessage,
CorrelationId, Timestamp
```

- One row per **field** changed (not one row per request) for update
  operations, so "what changed" is queryable at field granularity. A single
  `UpdateKycCommand` call that changes 3 fields writes 3 `AuditLogs` rows
  sharing one `CorrelationId`.
- For non-field operations (card activation, mock enable/disable), `Field`
  is null/N-A and `OldValue`/`NewValue` describe the operation's effective
  before/after state as a short string (e.g. `OldValue: "Disabled"`,
  `NewValue: "Enabled"`).

## 3. Where the audit write happens

The audit write is part of the same command handler pipeline (see
`docs/01-architecture.md` §4), executed after the underlying change succeeds
but before the handler returns:

```
handler:
  load current state
  compute diff (old vs. requested new values)
  apply change via provider/client
  if apply succeeded:
      write AuditLogs rows for the diff (Result = Success)
  else:
      write ONE AuditLogs row summarizing the attempted operation
      (Result = Failed, ErrorMessage = safe message)
  return Result to controller
```

For the **portal's own PostgreSQL-backed** entities (mock config, admin),
the change and the audit write happen in the same DB transaction — atomic.

For **enterprise MSSQL/external-API** operations, the target system is
outside the portal's transaction boundary, so the audit write cannot be
made atomic with it. The rule instead is: **the audit write is attempted
immediately after the external operation's outcome is known, and if the
audit write itself fails, that failure is logged at ERROR level via Serilog
and surfaced to the caller as a degraded-success response** (operation
succeeded, audit logging failed) rather than silently dropped — this keeps
"every mutation is audited" true in the overwhelming common case while being
honest about the one genuine cross-system atomicity gap, rather than
pretending a two-phase commit exists where it doesn't.

## 4. Masking rules

| Field pattern | Masking |
|---|---|
| National ID / Yakeen identifiers | Show first 2 and last 2 characters, mask the rest: `12******90` |
| Mobile number | Show last 4 digits: `*******1234` |
| Passwords, tokens, API keys | Never stored in `OldValue`/`NewValue` at all — the field is excluded from audit entirely (these are never user-editable through this portal's screens anyway) |
| Free-text name/address fields | Not masked — needed to verify the correct test change was made; still access-controlled via `audit.view` |

Masking is applied at the point the `AuditLogs` row is written (in the
Application-layer audit service), not at read time — so masked data is
never at rest unmasked.

## 5. Access to audit data

- `audit.view` permission required to query `/api/v1/audit`.
- Audit rows are append-only: no `PUT`/`DELETE` endpoint exists for
  `AuditLogs`. Corrections are new rows, never edits.
- Audit UI (Phase 7) supports filtering by user, environment, screen, date
  range, and customer/merchant identifier.

## 6. OperationLogs vs. AuditLogs

`OperationLogs` is a technical execution log (command name, duration,
result, correlation ID) used for performance/reliability monitoring.
`AuditLogs` is the business-facing "who changed what" record. They share a
`CorrelationId` so a slow/failed operation can be cross-referenced with its
business audit trail, but are queried and retained independently.
