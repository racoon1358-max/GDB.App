# GDB.App console-to-Web API migration

## Goal and scope

Migrate the .NET 8 console application's business operations to an ASP.NET Core Web API named `GDB.App.WebApi`. Preserve account rules and transaction behavior while making the API safe to call and test. Keep the console app buildable during migration; do not replace its `Main` with API hosting. Target a tested, access-controlled API staging release by **October 12, 2026**. **R8 is an optional console-as-API-client experiment within that same deadline, not a requirement for API completion.** Production release requires the security and data-integrity gates plus an agreed deployment destination.

Current baseline: one console project; no solution, automated test project, CI workflow, or SQL schema scripts in this repository. A SQL Server schema (including stored procedures) and safe test/staging database are available to the team. The console's `Application/Controllers` are ordinary classes, not HTTP controllers. Factories select DB repositories, and `App.config` contains the DB connection string. Create tests and CI rather than assuming existing suites or pipelines can be updated.

## Team, Git workflow, and versioning

- Three contributors use **one repository with three maintainers**, not three forks. Protect `main`; use short-lived `feature/*` branches and pull requests with at least one other contributor's review. Assign primary ownership to (1) API/contracts, (2) domain/data and transactions, and (3) tests/CI/deployment; cross-review contract, security, and money-movement changes. Merge only when the solution builds and relevant tests pass.
- Tag an API staging candidate `v0.1.0-rc.1`; tag `v0.1.0` after R6/R7 acceptance regardless of optional R8. Keep the Git tag version distinct from the HTTP API version. Establish `/api/v1` routes and a documented OpenAPI v1 contract in R5; add multi-version support only if a second contract is needed.
- Freeze the first-release endpoint list and request/response shapes in R0/R1: create/view/list/close account, balance, deposit, withdraw, transfer, and recent transactions. Explicitly define who may call each operation; do not expose all accounts or balances merely because the console menu can display them.

## Top-down, multi-dimensional approach

“Top-down” means start with the **outcome** (a usable, safe Web API), define observable behavior and release gates, then choose the projects, classes and tasks needed to deliver it. “Multi-dimensional” means assess every release from more than just a code-structure perspective:

| Dimension | Question to answer before accepting a change |
| --- | --- |
| Behavior and API contract | Which console operation does it preserve, what HTTP request/response represents it, and who may call it? |
| Architecture | Where do domain rules, application orchestration, SQL access and HTTP concerns belong without circular dependencies? |
| Data integrity | Do balances and transaction records remain correct after failures and concurrent requests? |
| Security | Are credentials protected, and are authentication **and** per-operation/account authorization enforced before wider access? |
| Verification | Which unit, SQL-backed, API and smoke tests demonstrate the behavior and failure cases? |
| Operations and rollout | Can the three contributors run it, does CI check it, and can staging be deployed and rolled back safely? |

These are **checks across R0–R7** (and the optional R8 experiment), not six additional sequential phases. For example, R2 must define endpoint permissions even though OAuth is implemented in R6; R3 must test SQL rollback, not merely compile after replacing factories.

## Timeline and effort

Assumption: each person has **1.3–2 hours per weekday and 3–4 hours per weekend day**. October 1–12 has eight weekdays and four weekend days: approximately **67–96 team-hours**. Estimates include implementation, reviews and tests; overlapping dates allow parallel work. Budget **74 h of R0–R7 work + 8 h contingency = 82 h**. If that work finishes early, cap optional R8 at **4 additional hours** within the same window (maximum planned use: **86 h**). At the lower end of team availability, R8 will not fit; drop it rather than defer API gates or extend the deadline.

| Release | Target dates | Effort | Deliverable / exit gate |
| --- | --- | ---: | --- |
| **R0/R1 — plan and structure** | Oct 1–2 | 8 h | Confirm existing SQL behavior, ownership, API permissions and OAuth client types **including the console**; create solution structure, API/test projects and a running health endpoint. Console still builds. |
| **R2 — copy, controllers, run** | Oct 3–5 | 16 h | Reuse existing logic without a wholesale rewrite; add thin HTTP controllers, DTOs, initial DI registrations and runnable endpoints. Start behavior tests; compare results with the console app. |
| **R3 — DI and factory removal** | Oct 5–7 | 14 h | Replace factory/static setup on the API path with injected services, repositories, logging and `IDbConnectionFactory`; securely configure SQL. Verify money movements and transaction records share an atomic persistence boundary. |
| **R4 — models and validation** | Oct 7–8 | 8 h | Validate requests and account-type rules; test invalid amounts, PINs, account state, missing accounts and duplicate/invalid account data. |
| **R5 — API reliability and versioning** | Oct 8–10 | 10 h | Centralize error handling, logging and middleware; publish `/api/v1` OpenAPI; add API integration tests and CI build/test. Staging smoke tests pass with restricted access. |
| **R6 — OAuth and authorization** | Oct 10–11 | 12 h | Validate provider-issued access tokens and enforce operation- and account-level permissions. Test unauthenticated, forbidden and cross-account requests. No unrestricted deployment. |
| **R7 — Docker containerization** | Oct 10–12 | 6 h | Build/run the Web API container, provide non-secret configuration and a staging smoke-test procedure; verify R6 controls also work in the container. |
| **R8 — optional console experiment** | By Oct 12, only after R7 | Up to 4 h | Prototype **one** console menu action calling an authenticated API endpoint over HTTP; no full menu migration or release gate. |
| **Contingency** | Throughout | **8 h** | Protect R0–R7 for SQL differences, identity-provider setup, integration failures and review fixes; do not spend it on R8. |

If a mandatory gate slips, skip R8 and use the remaining time for a restricted staging/demo build; do not weaken account authorization or transaction correctness to meet October 12.

## Release details

For implementation order, suggested commands, verification and handoff steps within each release, see [`MIGRATION_GUIDE.md`](MIGRATION_GUIDE.md). This plan remains the source of truth for scope, schedule, estimates and release gates.

### R0/R1 — plan and project structure (8 h)

- **R0: agree on the target.** Inventory the nine first-release operations, SQL tables/stored procedures they use, console responses and error cases. Record endpoint contracts and which identity may perform each operation; decide the OAuth client types (including a possible console public client for R8) and choose an identity provider (Keycloak recommended, not yet selected). Identify staging owner, deployment destination, and cutover approver.
- **R1: establish a buildable baseline.** Add a solution containing the unchanged console project, an ASP.NET Core Web API project targeting .NET 8, and test projects. Start the API via its own `Program.cs`, register an initial health endpoint, and run a first build/test in CI or document the temporary manual command until CI is added in R5. Do not create an API by repurposing the console `Main`.
- **Exit:** both hosts build, the API starts, the endpoint/permissions list and ownership decisions are written down, and each contributor has access to the safe SQL environment. An undecided identity provider is explicitly tracked with an owner and decision date before R6.

### R2 — copy behavior, controllers and runnable API (16 h)

- Create API request/response DTOs and thin `[ApiController]` endpoints for the agreed operations. Delegate to existing application services/commands; add only enough DI registrations/adapters to run them. Preserve domain checks and existing SQL mappings instead of rewriting everything at once.
- Add focused tests for representative account and transaction behavior and compare API results with the console flows. Keep the console app operational and keep this API restricted to local/test staging: no endpoint is considered publicly releasable before R6.
- **Exit:** a contributor can run the API and exercise the agreed happy paths against the safe test database; console and API builds pass. Record gaps rather than silently removing an operation to meet the date.

### R3 — complete DI and remove factory-based API wiring (14 h)

- Replace hardcoded `"DB"` selection, static connection management and static logging **on the API path** with registered services/repositories, `IDbConnectionFactory` and injected `ILogger<T>`. Load the connection string from non-committed configuration. Extract Domain/Application/Infrastructure libraries incrementally only when their references are acyclic.
- Refactor deposit, withdrawal and transfer persistence so each balance update and its transaction record commit or roll back together. Check affected rows and test concurrent changes rather than assuming `SaveAccounts` alone makes transfers atomic.
- **Exit:** API behavior no longer depends on factories or `App.config`; SQL-backed tests prove rollback on a failed transaction-record write and no lost money on competing updates. Console remains buildable.

### R4 — models and validation (8 h)

- Define input requirements for account numbers, account types, amounts, PIN submission, transfers and account creation. Validate at the HTTP boundary, then keep business rules in the domain; do not duplicate or weaken account-type-specific constraints. Ensure response DTOs omit PINs and internal database details.
- Add tests for malformed inputs, nonexistent or inactive accounts, invalid PINs, duplicate accounts, insufficient balance and transfer-to-self, alongside successful cases for the four account types.
- **Exit:** invalid input gets a predictable client error without modifying balances; valid inputs preserve documented console behavior. Any intentional behavior change is recorded in the API contract.

### R5 — errors, logging, middleware and versioning (10 h)

- Introduce centralized exception handling and consistent problem responses; map failures by their meaning rather than assigning one status to all `AccountException` instances. Add structured logging without PINs, credentials or full account records. Publish `/api/v1` routes and OpenAPI v1 documentation; this is an initial contract, not a requirement for a multi-version framework.
- Add `WebApplicationFactory` tests for HTTP responses and errors. Configure pull-request CI to restore, build and run unit/API tests; run SQL-backed tests in a separate securely configured job or documented local/staging step. Run restricted staging smoke tests and verify the console app still builds.
- **Exit:** API contract and error mappings are reviewable, automated checks pass, and failures do not expose stack traces or secrets.

### R6 — OAuth authentication and authorization (12 h)

- Integrate the identity provider selected in R0/R1. Configure the API to validate token signature, issuer, audience and lifetime; do not issue tokens in the API or treat an account PIN as an OAuth credential. If R8 is attempted, use a test token obtained through a supported interactive public-client flow (Authorization Code + PKCE or Device Authorization); never embed a client secret in the console. Full console login integration is outside the time-boxed experiment.
- Enforce scopes/roles for operations **and** caller-to-account access for account lookups, lists and transfers. Test missing/invalid/expired tokens, insufficient permissions and cross-account access, including the interactive API documentation surface.
- **Exit:** no sensitive endpoint is accessible anonymously or merely with someone else's valid token. If provider setup or account ownership rules remain unresolved, keep the build in restricted staging and move the release target.

### R7 — Docker containerization and release check (6 h)

- Add a reproducible, preferably multi-stage Web API container build; pass SQL/OAuth settings at runtime rather than baking secrets into the image. Document build/run commands, database/network prerequisites and a basic health check. Keycloak may have been run separately since R0/R1; R7 is about packaging **the Web API**.
- Run the container against the safe staging database and chosen provider; smoke-test authorized and rejected requests plus one end-to-end transaction. Document how to redeploy the previous image and keep the console fallback until cutover approval.
- **Exit:** the container starts, passes the R6 security checks and staging smoke tests, and has a documented rollback path. Tag the API release after these gates pass, whether or not R8 is attempted; production routing remains a separate approval.

### R8 — optional console-to-API experiment (up to 4 h)

- **Prerequisite:** R6/R7 gates have passed and unused capacity remains before October 12. Do not displace mandatory fixes for this experiment.
- Prototype **one read-only `Home` menu action** (for example, view balance) using a small `HttpClient` adapter to call the corresponding authenticated `/api/v1` endpoint. The experimental path is **console → HTTP → Web API controller → application service**; the other menu actions remain unchanged. Configure the API URL outside source and supply a short-lived test user's bearer token via a safe local mechanism, not a committed file or embedded client secret. Handle a failed response/timeout without displaying a false success.
- Verify the chosen operation against the staging API and document what a future full console migration would require (interactive OAuth login, remaining menu actions, failure mapping, tests and removal of direct SQL startup). Do **not** remove `DataBaseProviderRegistration.Register()` or claim the console is HTTP-only while other menu actions still depend on SQL.
- **Exit:** one demonstrable authenticated console-to-API call, or a recorded decision to skip R8 because time ran out. R8 does not gate the API tag or require extending the October 12 timeline.

## Implementation decisions across releases

- **Architecture:** In R1 create the solution and `GDB.App.WebApi` alongside the console project. During R2/R3, move `Domain`, `Application` and `Infrastructure` into separate libraries only in buildable increments: domain models/rules; DTOs/services/commands; SQL repositories/connection management. The present infrastructure code refers to application DTOs, so resolve project-reference direction before splitting rather than copying files into circularly dependent projects. Place only HTTP controllers in the Web API project; R8 demonstrates one console-to-HTTP call, not an HTTP-only console.
- **SQL and transaction semantics:** Retain ADO.NET and the existing schema/stored procedures for the first release. `SaveAccounts` currently commits balances before `SaveTransaction` opens another connection; deposit/withdraw likewise update balances separately from recording the transaction. Make each money movement and its transaction record atomic, check affected rows, and test rollback and concurrent updates against the safe SQL database. Existing `GetAccountAsync` is async, but many writes are synchronous: convert only where needed for the scoped migration, propagating cancellation tokens consistently when doing so. Do not make an ORM migration part of the deadline.
- **Configuration and secrets:** Replace `App.config`/`ConfigurationManager` on the API path with typed configuration and an injected connection factory. Commit only non-secret settings; supply DB credentials through environment variables or a development secrets store, not committed `appsettings.json`, launch settings, Docker files or logs. Use injected `ILogger<T>` instead of static `AppLogger`. Keep local launch settings non-secret.
- **HTTP contract:** Use `[ApiController]`, explicit request/response DTOs, and validation rather than exposing domain entities or PIN fields in responses. Document routes and status codes in OpenAPI. Centralize errors with `UseExceptionHandler`/problem details: distinguish not-found (404), invalid input (400), conflicts such as inactive accounts (409), and unauthorized (401)/forbidden (403). Do not map every `AccountException` to a single status or return stack traces to clients. Enable interactive API documentation only in development or appropriately protected environments.
- **OAuth security:** OAuth 2.0 / OpenID Connect is the chosen approach; the API **validates provider-issued access tokens** rather than issuing its own JWTs. Decide the identity provider, actual client types, scopes/roles, and account-ownership rules in **R0/R1**, even though API validation is implemented in **R6**. **Provider remains an open decision:** Keycloak is recommended for training because it can run locally, gives the three contributors a reproducible OAuth/OIDC setup, and avoids building a custom token service. Confirm that all contributors can run it and configure clients in time; running Keycloak early does not move the API's Docker deliverable out of R7. A PIN used by existing transaction rules is not API authentication. Restrict pre-R6 instances to safe test/staging access; do not put tokens or client secrets in source for R8.
- **Verification and delivery:** Add unit tests for domain/service rules and SQL-backed tests for atomicity; add `WebApplicationFactory` integration tests for HTTP contracts and authorization. Create CI to restore, build and test the solution on pull requests; keep database-dependent tests isolated and provide their connection settings securely. Run staging smoke tests for each first-release operation and failure case. Swagger/manual checks supplement, not replace, automated tests. R8 is a limited demonstration, not console parity or retirement of the direct-DB path.

## Open decisions and release risks

1. **R0/R1:** Which identity provider (Keycloak recommended), which client flows will the real API consumers use, and what account-level permissions are required? If the provider cannot be ready in time, October 12 is a restricted API staging/demo milestone and R8 is skipped.
2. **R0/R1:** Where will staging/production run, and who approves console cutover? Docker packaging alone is not deployment or a rollback plan. Keep the console path available until consumers and rollback are confirmed.
3. **R3:** Confirm SQL isolation/concurrency strategy and stored-procedure behavior on the available database. Separate writes and read-modify-write races are release-blocking for money movement.
