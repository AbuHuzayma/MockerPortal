# 08 — Integration Architecture

## 1. Principle

Application layer code depends only on interfaces. Concrete implementations
(real MSSQL provider, real HTTP client, or a mock/in-memory adapter) are
supplied by dependency injection, chosen per environment/configuration. This
is what lets the project run locally without any enterprise system, and lets
real integrations be dropped in later without touching Application or Api
code.

```
Application layer:      ICustomerProvider  (interface)
                              ▲
                  ┌───────────┴────────────┐
Infrastructure:  SqlCustomerProvider   MockCustomerProvider
                  (Dapper → MSSQL)      (in-memory / seeded fixtures)
```

DI registration (in `Portal.Api` composition root) picks the implementation
based on `appsettings.{Environment}.json` (e.g. `Providers:Customer:Mode =
"Sql" | "Mock"`), so DEV can run entirely on mocks while QA/PREPROD use real
providers, without a code change.

## 2. Database-backed abstractions

```csharp
ICustomerProvider          // T_PRT_CUSTOMER reads + the fields the KYC/
                            // creation/security/biometric commands touch
IKycProvider                // may be the same implementation as ICustomerProvider
                            // depending on final schema; kept as a separate
                            // interface so KYC can be re-pointed independently
IIvrProvider                 // IVR database — schema unknown, mock-only until supplied
IBiometricProvider            // Biometric module DB
IMerchantProvider              // Merchant DB
```

Each has exactly one Infrastructure implementation per data source type
(Sql*, Mock*). Connection strings are named per logical database (see
`docs/05-database-design.md` §3 and `docs/12-environments.md`), never
shared across providers.

## 3. Web API-backed abstractions

```csharp
ICardManagementClient          // CreateCard(...), ActivateCard(...)
IBeneficiaryServiceClient       // ActivateBeneficiary(...)
IB2BServiceClient                // AddMerchantToB2B(...)
IIdentityVerificationClient       // VerifyYakeen(...), etc. — Absher/Yakeen/ELM
```

Implementations live in `Portal.Integrations`, built on `HttpClientFactory`
+ Polly (timeout + retry with jittered backoff + circuit breaker). Each
client:

- Is registered via `AddHttpClient<TClient, TImpl>()` with a named/typed
  client and a base address from environment-specific configuration.
- Exposes explicit, narrow methods (`ActivateCard(request, ct)`) — never a
  generic `Send(HttpRequestMessage)` passthrough reachable from the API
  surface.
- Generates/propagates a correlation ID on every outbound call.
- Logs method, target endpoint, status code, and duration — never request/
  response bodies that may contain secrets or full PII (mask before
  logging).

### Mock mode

For `IIdentityVerificationClient` in particular (Absher/Yakeen/ELM), and
optionally for the others, the same interface has a "mock mode"
implementation that calls this project's own **API Mocker** engine
(`docs/09-api-mocker.md`) instead of the real external endpoint — selected
by the same `Providers:*:Mode` configuration pattern as database providers.
This means QA can test the full command flow (`VerifyYakeen` →
`UpdateKycCommand`) end-to-end against a configured mock response without
any real government/partner connectivity.

## 4. Unknown contracts

Until real Swagger/contract documents are supplied for Card Management,
Beneficiary Service, B2B Subscription Matrix, and Absher/Yakeen/ELM, the
Integrations project:

- Defines the interface method signatures based only on the operations named
  in the master spec (`CreateCard`, `ActivateCard`, `ActivateBeneficiary`,
  `AddMerchantToB2B`).
- Implements them against a documented **assumed** minimal request/response
  DTO shape, clearly marked `// ASSUMED CONTRACT — replace when real API
  spec is supplied` in code, and mirrored in this doc's "Assumed contracts"
  appendix (added once Phase 4/5/6 implementation starts).
- Ships a mock/local implementation so the command layer and UI can be built
  and tested without the real API.

No enterprise API base URL, auth scheme, or field name is invented beyond
what's needed to compile a plausible, clearly-marked placeholder.

## 5. Resilience policy defaults (Polly)

| Concern | Default |
|---|---|
| Timeout per attempt | 10s (configurable per client) |
| Retry | 2 retries, exponential backoff with jitter, only on transient (5xx/timeout), never on 4xx |
| Circuit breaker | Opens after 5 consecutive failures, 30s break |

Overridable per client via configuration where a specific downstream needs
different behavior (documented inline where it deviates from the default).
