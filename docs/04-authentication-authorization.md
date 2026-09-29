# 04 — Authentication & Authorization

## 1. Authentication (Phase 1: local only)

- **Mechanism**: ASP.NET Core Identity (email/password) against the portal's
  PostgreSQL database.
- **Password policy**: minimum length 12, requires mixed case + digit +
  symbol (configurable via Identity options), no maximum age requirement by
  default (can be added if the org requires it).
- **Lockout**: 5 failed attempts → 15 minute lockout (Identity's built-in
  lockout, configurable).
- **Tokens**: on successful login, issue a JWT access token (short-lived,
  default 15 min) signed with a key from configuration/secret store, plus a
  refresh token (longer-lived, stored hashed server-side in
  `RefreshTokens` table, single-use/rotated on refresh).
- **Endpoints**:
  - `POST /api/v1/auth/login` — email + password → access + refresh token.
  - `POST /api/v1/auth/refresh` — refresh token → new access + refresh token.
  - `GET  /api/v1/auth/me` — current user, roles, effective permissions,
    current environment.
  - `POST /api/v1/auth/logout` — revokes the refresh token.

### Future: Entra ID / AD

The login flow is abstracted behind an `IAuthenticationProvider`-shaped
boundary (concretely: Identity's `SignInManager`/token issuance is called
from a dedicated `AuthService`, not scattered across controllers). Adding
Entra ID later means adding an OIDC-based provider that, on successful
external sign-in, resolves/creates a local `User` row and issues the same
internal JWT shape — downstream authorization code is unaffected because it
only ever sees the internal JWT + claims, never the identity provider
directly. No redesign is required; this is a documented extension point, not
a promise about scheduling.

## 2. Authorization model

**RBAC + granular permissions**, both enforced server-side.

```
User ──< UserRoles >── Role ──< RolePermissions >── Permission
```

- A `Permission` is a flat string (e.g. `customer.kyc.update`,
  `customer.kyc.update.preprod`). Permissions are seed data + admin-managed,
  not hardcoded enums scattered through controllers (controllers reference a
  small set of `PermissionCodes` constants for compile-time safety, backed
  by the same string values stored in the DB).
- A `Role` is a named bundle of permissions. Seed roles: `Administrator`,
  `QA`, `Developer`, `ReadOnly` (see table below for the seed mapping).
- A `User` has one or more `Role`s. Effective permissions = union of all
  assigned roles' permissions.
- **Environment-scoped permissions**: a permission suffixed `.qa` or
  `.preprod` (e.g. `customer.security.remove.preprod`) is checked against
  the request's resolved environment (`IEnvironmentContext`) in addition to
  the base permission. A user without the environment-suffixed permission is
  forbidden even if they hold the base permission, for the operations that
  define one (initially: `customer.kyc.update`, `customer.security.remove`,
  `api-mocker.enable`, per the master spec). **`DEV` is exempt from the extra
  check** — no `.dev` permission variant exists anywhere in `PermissionCatalog`
  (only `.qa`/`.preprod`, matching every example in the master spec), so
  `PermissionAuthorizationHandler` skips the scoped check entirely when
  `IEnvironmentContext.Name == "DEV"`. Without this, an environment-scoped
  action could never succeed locally, since no permission code could ever
  satisfy it. Found and fixed during Phase 4's KYC implementation, the first
  environment-scoped write path actually exercised end-to-end.
- A role granted a `.qa`/`.preprod`-scoped permission must **also** hold the
  matching base permission — the scoped check is additive, not a
  replacement (see `IdentitySeeder`'s `QaExtraPermissions`, which grants
  `customer.kyc.update` alongside `customer.kyc.update.qa` for exactly this
  reason). A scoped-only grant is inert.

### Seed role → permission mapping (Phase 1 defaults, adjustable via admin UI later)

| Role | Permissions |
|---|---|
| Administrator | all permissions, all environments |
| QA | `*.view`, `customer.kyc.update.qa`, `customer.ivr.update`, `customer.creation.update`, `customer.otp.update`, `customer.card.activate`, `customer.beneficiary.activate`, `customer.biometric.update`, `merchant.update`, `merchant.b2b.update`, `api-mocker.*` (non-preprod) |
| Developer | same as QA plus `api-mocker.manage`, broader mock permissions |
| ReadOnly | every `*.view` permission only |

PREPROD-scoped and security-lock permissions are **not** granted to QA/
Developer by default seed data — an Administrator must explicitly grant them
per the master spec's sensitivity (`customer.security.remove` is PREPROD-only
and highly sensitive).

## 3. Enforcement mechanism

- ASP.NET Core policy-based authorization: one policy per permission string,
  registered dynamically at startup from the `PermissionCodes` catalog, or a
  single custom `IAuthorizationHandler` (`PermissionAuthorizationHandler`)
  that reads a `[HasPermission("customer.kyc.update")]` attribute and checks
  the current user's claims — implementation choice made in Phase 1, must
  support both the base and environment-scoped forms.
- Permissions are embedded in the JWT as claims at login time (and refreshed
  on token refresh), so authorization checks don't require a DB round-trip
  per request. Role/permission changes take effect on next login/refresh —
  documented, acceptable latency for an internal tool.
- `GET /auth/me` returns the effective permission list so the frontend can
  do UX-level hide/show — this is explicitly **not** a security boundary
  (see `docs/03-security.md` §2).

## 4. Frontend responsibilities (UX only)

- Route guards redirect users away from screens they lack `*.view` for.
- Buttons/actions are disabled/hidden when the user lacks the relevant
  permission.
- The frontend never assumes an action is safe to attempt just because it's
  visible — it still handles 403 responses gracefully (toast/error state).

## 5. Implemented in Phase 1

- Refresh-token storage: **httpOnly, `SameSite=Lax` cookie**, scoped to path
  `/api/v1/auth` (never sent on unrelated requests), `Secure` outside
  Development. The access token is returned in the JSON response body and
  kept **in memory only** on the frontend (`src/auth/tokenStore.ts`) — never
  `localStorage`/`sessionStorage` — so a page reload always re-establishes
  the session via a silent `POST /auth/refresh` call rather than trusting
  client-side storage.
- `PermissionPolicyProvider` (dynamic `IAuthorizationPolicyProvider`) +
  `PermissionAuthorizationHandler` + `[HasPermission(code, environmentScoped:
  true)]` attribute — one requirement class handles both the base and
  environment-scoped permission checks (`Portal.Api/Authorization/`).
- Seed roles/permissions run in every environment (idempotent); the four dev
  seed users (`admin@portal.local`, `qa@portal.local`,
  `developer@portal.local`, `readonly@portal.local`) are Development-only.
  See `README.md` for the seed password.

## 6. Unknowns

- Whether the org requires SSO (Entra ID) before go-live or only later —
  affects nothing architecturally, only sequencing.
