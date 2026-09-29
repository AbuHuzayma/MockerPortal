# 07 — Dynamic Screen Engine

## 1. Purpose

Let screen layout (fields shown, labels, order, control type, required/
editable/visible, which permission gates a field) be configured via metadata
rather than hardcoded per-screen React markup — while keeping the actual
business write operation a fixed, reviewed backend command.

**This is presentation metadata, not a generic CRUD/workflow engine.** It
never determines what SQL or API call happens; it only determines what the
form looks like and what's visible/editable to the current user.

## 2. Metadata model

See `docs/05-database-design.md` §2 for the table shapes: `Screens`,
`ScreenFields`, `ScreenActions`, `ScreenPermissions`.

```
Screen (code: CUSTOMER_KYC)
 ├─ ScreenField (FieldKey: EMAIL, ControlType: Text, Required: true, ...)
 ├─ ScreenField (FieldKey: LIFE_STATUS, ControlType: Select, ...)
 ├─ ScreenAction (Code: SAVE, Permission: customer.kyc.update)
 └─ ScreenPermission (customer.kyc.view)
```

`GET /api/v1/screens/{code}` returns the screen definition filtered to the
requesting user's permissions. **Settled in Phase 3:** a field or action
whose `Permission` the caller lacks is **omitted entirely** from the
response — never included-but-disabled — so field/action existence is never
leaked beyond what's necessary (`ScreenService` in
`Portal.Infrastructure/Screens`). A screen-level `ScreenPermissions` gate
additionally requires the caller to hold *every* listed permission to
receive the screen definition at all; failing that returns
`SCREEN_FORBIDDEN` (403) rather than `SCREEN_NOT_FOUND` (404), which is
reserved for a genuinely unknown screen code.

## 3. Backend: metadata vs. command

Metadata drives the **form**. The **command** (e.g. `UpdateKycCommand`)
independently defines its own allowed property set in code (a C# record with
explicit properties, validated by a FluentValidation validator). The
mapping from `ScreenField.FieldKey` → command property is explicit
(`ScreenField.IntegrationKey`), and the command handler ignores any incoming
field it doesn't recognize.

Concretely, adding a `ScreenField` row for a new column does **not** by
itself allow that column to be written — a developer must also add the
property to the command, its validator, and the provider's update method.
This is a deliberate friction point: UI metadata changes are configuration,
data-write capability changes are code review.

## 4. Frontend: DynamicForm / DynamicField

```
src/components/dynamic-form/
  DynamicForm.tsx    -- fetches screen metadata, builds a Zod schema from
                         ScreenField definitions, renders fields, handles the
                         confirmation dialog, calls the caller-supplied
                         onSubmit(actionCode, values)
  DynamicField.tsx    -- renders one field by ControlType (Text, Number,
                         Decimal, Date, DateTime, Boolean, Checkbox, Radio,
                         Select, MultiSelect, TextArea, ReadOnly)
  buildSchema.ts        -- ScreenField[] → Zod schema
  types.ts                -- ScreenDefinition/ScreenField/ScreenAction, mirroring the API DTOs
```

`DynamicForm` does not fetch or POST record *data* itself (only the screen's
*metadata*) — the caller passes in `values` (fetched from whatever
screen-specific endpoint holds that data, e.g. `GET /sample-screen/record` in
Phase 3, `GET /customers/{id}/kyc` in Phase 4) and an `onSubmit` callback
that calls that screen's real command endpoint. This keeps `DynamicForm`
generic across screens whose data lives in entirely different places.

- `DynamicForm` builds a React Hook Form instance whose Zod schema is
  derived at runtime from `ScreenField.DataType`/`Required` — client-side
  validation mirrors but never replaces server-side FluentValidation.
- Permission-gated fields are simply absent from the API response the
  frontend receives (see §2) — `DynamicForm` never has to special-case
  "hide this field," it only ever renders what it was given.
- `ScreenAction`s with `RequiresConfirmation: true` render a confirmation
  dialog showing current vs. new value per the UX rule in
  `docs/11-ui-design.md` §"Mutation confirmation pattern", before calling
  the endpoint.
- `Select`/`MultiSelect` options come from `ScreenField.Options`
  (`docs/05-database-design.md`'s `OptionsJson` column), an array of
  `{value, label}` pairs.

## 5. Supported control types

`Text, Number, Decimal, Date, DateTime, Boolean, Checkbox, Radio, Select,
MultiSelect, TextArea, ReadOnly` — matches the master spec exactly; no
additional control types are introduced without updating this doc.

## 6. What this engine deliberately does not do

- No conditional/branching logic between fields (e.g. "show field B only if
  field A = X") in v1 — out of scope until a concrete screen needs it and
  it's documented here first.
- No dynamic **actions** beyond calling one fixed backend command per
  `ScreenAction` — no generic scripting, no workflow chaining. (This is the
  boundary that keeps this from becoming the removed "generic
  workflow/scenario engine.")
- No dynamic creation of new backend write capabilities purely through
  admin UI — every write path is code-reviewed.

## 7. Phase 3 deliverable ✅ complete

One working sample screen — `SAMPLE_SCREEN` (`ScreenSeeder`, seeded
idempotently in every environment) — proving the metadata → API →
DynamicForm round trip, reachable at `/dev/sample-screen` (linked from the
dashboard, not the main nav — it isn't a real business screen). It exercises:

- Every supported `ControlType`, including a `ReadOnly` field and a
  `Select` field with `Options`.
- Field-level permission omission: one field (`internalNote`) requires
  `admin.users` and is absent from the response for non-Administrators.
- The confirmation dialog (`ScreenAction.RequiresConfirmation = true`).
- The full write-operation pattern (CLAUDE.md §36) end-to-end: validation
  (`SampleRecordValidator`) → load current state → apply the change → write
  an `AuditEntry` per changed field → return the result — backed by
  `ISampleScreenService`, an in-memory singleton that is explicitly *not* a
  template for real screens (those call a real provider, starting Phase 4).
