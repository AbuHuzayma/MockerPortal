# 05 — Database Design

## 1. Two categories of data

1. **Portal database** (PostgreSQL, owned by this project, EF Core migrated).
2. **Enterprise databases** (MSSQL, owned by other systems, accessed via
   Dapper/ADO.NET through named providers — never migrated or schema-owned
   by this project).

## 2. Portal database (PostgreSQL) — ERD

```
Users ──< UserRoles >── Roles ──< RolePermissions >── Permissions
  │
  └─< RefreshTokens

Screens ──< ScreenFields
       ──< ScreenActions
       ──< ScreenPermissions

Integrations ──< IntegrationEndpoints
             ──< IntegrationMappings

MockApis ──< MockEndpoints ──< MockResponses ──< MockMatchRules

AuditLogs (append-only, references UserId; CustomerId/MerchantId are
           opaque string references to enterprise-DB keys, not FKs)

OperationLogs (technical/operational log of command executions, distinct
               from the business-facing AuditLogs)
```

### Core tables

```
Users
-----
Id (uuid, pk)
Email (unique)
NormalizedEmail
PasswordHash
FullName
IsActive
CreatedAt
UpdatedAt
-- + standard ASP.NET Identity columns (SecurityStamp, ConcurrencyStamp, etc.)

Roles
-----
Id (uuid, pk)
Name (unique)          -- Administrator, QA, Developer, ReadOnly, ...
Description

UserRoles
---------
UserId (fk Users)
RoleId (fk Roles)
PRIMARY KEY (UserId, RoleId)

Permissions
-----------
Id (uuid, pk)
Code (unique)           -- e.g. customer.kyc.update, customer.kyc.update.preprod
Description
Category                -- Customer, Merchant, ApiMocker, Audit, Admin

RolePermissions
----------------
RoleId (fk Roles)
PermissionId (fk Permissions)
PRIMARY KEY (RoleId, PermissionId)

RefreshTokens
-------------
Id (uuid, pk)
UserId (fk Users)
TokenHash
ExpiresAt
CreatedAt
RevokedAt (nullable)
ReplacedByTokenHash (nullable)
```

### Dynamic screen metadata

```
Screens
-------
Id (uuid, pk)
Code (unique)            -- e.g. CUSTOMER_KYC
Name
Description
Category                 -- Customer, Merchant, ApiMocker, Admin
IsActive
DisplayOrder

ScreenFields
------------
Id (uuid, pk)
ScreenId (fk Screens)
FieldKey                 -- e.g. EMAIL, FIRST_NAME
Label
DataType                 -- Text, Number, Decimal, Date, DateTime, Boolean
ControlType              -- Text, Number, Decimal, Date, DateTime, Boolean,
                          -- Checkbox, Radio, Select, MultiSelect, TextArea, ReadOnly
Required
Editable
Visible
DisplayOrder
Permission                -- required permission to see this field at all (absent
                          -- if lacking it — omitted from the API response entirely,
                          -- not merely disabled; see docs/07 §2)
IntegrationKey             -- maps to the backend command's field name
OptionsJson                -- nullable JSON array of {"value","label"} pairs, only
                          -- meaningful for Select/MultiSelect. Added during Phase 3
                          -- implementation — the master spec named these control
                          -- types but never specified how their options are
                          -- supplied; this is the minimal schema addition that
                          -- makes them actually renderable.

ScreenActions
-------------
Id (uuid, pk)
ScreenId (fk Screens)
Code                      -- e.g. SAVE, RESET_LOCK
Label
Permission
RequiresConfirmation

ScreenPermissions
------------------
Id (uuid, pk)
ScreenId (fk Screens)
Permission
```

### Integration metadata

```
Integrations
------------
Id (uuid, pk)
Code (unique)              -- e.g. CARD_MANAGEMENT, BENEFICIARY_SERVICE
Name
Type                        -- Database, WebApi
Environment
IsActive

IntegrationEndpoints
---------------------
Id (uuid, pk)
IntegrationId (fk Integrations)
Code
BaseUrlConfigKey            -- name of the config/env var holding the base URL
                             -- (never the URL itself, if considered sensitive)

IntegrationMappings
--------------------
Id (uuid, pk)
IntegrationId (fk Integrations)
SourceField
TargetField
Notes
```

### API Mocker

```
MockApis
--------
Id (uuid, pk)
Code (unique)               -- ABSHER, YAKEEN, ELM, ...
Name
Description
Environment
IsActive
CreatedAt
UpdatedAt

MockEndpoints
-------------
Id (uuid, pk)
MockApiId (fk MockApis)
Path
HttpMethod
IsActive

MockResponses
-------------
Id (uuid, pk)
MockEndpointId (fk MockEndpoints)
Name
HttpStatusCode
ResponseHeaders (jsonb)
ResponseBody (text)
DelayMilliseconds
IsActive
Priority                     -- lower evaluates first among matching responses

MockMatchRule
-------------
Id (uuid, pk)
MockResponseId (fk MockResponses)
Source                       -- Header, QueryString, Path, RequestBody
Field
Operator                     -- Equals, NotEquals, Contains, StartsWith
ExpectedValue
```

### Audit

```
AuditLogs
---------
Id (uuid, pk)
UserId (fk Users, nullable for system actions)
Username                     -- denormalized snapshot at time of action
Environment
CustomerId (nullable, string — opaque enterprise key)
MerchantId (nullable, string — opaque enterprise key)
Screen
Operation
Entity
Field
OldValue (masked where sensitive)
NewValue (masked where sensitive)
Result                       -- Success, Failed
ErrorMessage (nullable, safe/user-facing only)
CorrelationId
Timestamp

OperationLogs
-------------
Id (uuid, pk)
CorrelationId
CommandName
DurationMs
Result
Timestamp
```

## 3. Enterprise MSSQL databases (accessed, not owned)

Per the master spec, only fields explicitly enumerated there are known.
Everything else is an **unknown**, implemented behind an interface with a
documented mock adapter until real schema is supplied.

| Logical DB | Known table(s)/fields | Status |
|---|---|---|
| CustomerDatabase | `T_PRT_CUSTOMER` — full field list in `docs/02-requirements.md` §KYC / master spec §10 | Fields known; exact column types/nullability/constraints unknown |
| CustomerDatabase (creation) | `DATE_CREATED`, `BIRTH_DATE`/`CUST_BIRTH_DATE`, `KYC_OK_DATE`/`KYC_LEVEL_DATE` | Field names known, source table/exact DB TBD |
| CustomerDatabase (security) | `FAILED_LOGON_COUNT`, `FAILED_OTP_COUNT`, `CURRENT_OTP_STATUS`, `FREEZE_STATUS_ID` | Fields known |
| CustomerDatabase (Yakeen) | `YAKEEN_STATUS_ID`, `YAKEEN_CHECK_DATE`, `YAKEEN_NEXT_DATE` | Fields known |
| IvrDatabase | — | Entirely unknown; architecture supports it (`IIvrProvider`), no fields invented |
| BiometricDatabase | — | Unknown; `IBiometricProvider` interface only |
| MerchantDatabase | `NAME_EN`, `NAME_AR`, `BRAND_NAME_EN`, `BRAND_NAME_AR`, `CR_EXPIRY_DATE`, `ID_EXPIRY_DATE` | Fields known |

Connection strings for these are never invented; only the ones actually
supplied/required are configured (`docs/12-environments.md`).

### KYC data modeling (Phase 4)

The ~68 KYC fields (master spec §10) are modeled as a key→value bag
(`Portal.Domain.Customers.KycRecord.Fields: IReadOnlyDictionary<string,
object?>`), not ~68 hand-written POCO properties. `Portal.Application.
Customers.KycFieldCatalog` is the single authoritative list — field key,
`T_PRT_CUSTOMER` column name, and label — driving both the `SqlKycProvider`
column mapping and the `CUSTOMER_KYC` screen's seeded `ScreenField` rows.
Adding a row to the catalog (and nowhere else) is what grants write
capability for a field; a `ScreenField` alone does not (docs/07 §3). This
was a deliberate choice over a typed POCO: every field is a plain
pass-through value with no per-field business logic, so a generic bag
matches the actual shape of the problem. The four `PEP_BY_*` flags are the
one exception, carrying real `bool` values; `PEP` itself is never stored —
it's computed (`KycRecord.ComputePep()`) as the OR of the four flags and
injected into API responses as a display-only field.

## 4. Related/child entities (not flattened onto Customer)

Per the master spec, the following are **not** columns on
`T_PRT_CUSTOMER` and must be modeled as child entities/sections once real
schema is available, not guessed at:

- `other_resident_*` fields
- `CONTACT_PERSON_*` fields

Until schema is supplied, the KYC screen/command exposes only the fields
explicitly listed in the master spec; these relational sections are called
out as "not yet implemented — pending schema" in the UI rather than faked.

## 5. Migrations

- Portal DB: EF Core Code-First migrations, committed to
  `backend/Portal.Infrastructure/Migrations`. Applied via `dotnet ef
  database update` (or automatically in local/dev startup, gated behind a
  `Database:AutoMigrate` config flag — never auto-applied in PREPROD).
- Enterprise MSSQL DBs: never migrated by this project.
