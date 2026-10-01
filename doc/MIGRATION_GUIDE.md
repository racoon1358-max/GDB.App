# GDB.App migration execution guide

Use this with [`PLAN.md`](PLAN.md), which owns the scope, dates, effort estimates and release gates. This guide describes **how to approach** each release; commands referring to projects not yet created are examples, not claims that those projects exist. Keep changes small enough to build and review. The existing console app remains intact until cutover is approved.

Effort is in **person-hours** and includes implementation, focused tests and review fixes. Assign each `TBD` at the release kickoff; estimates are planning aids, not individual deadlines. Each release table totals the release estimate in `PLAN.md`. Contributors implement assigned work on `task/rN-*` branches and open PRs into the current `release/rN-*` branch.

## Branch workflow example

Use one release integration branch at a time. The commands below use R3; replace the names for other releases.

1. One contributor creates the release branch from current `main`:

   ```bash
   git switch main
   git pull --ff-only origin main
   git switch -c release/r3-di
   git push -u origin release/r3-di
   ```

2. Each contributor creates a separate task branch from that release branch:

   ```bash
   git fetch origin
   git switch --create task/r3-atomic-transactions origin/release/r3-di
   git push -u origin task/r3-atomic-transactions
   ```

   Other R3 examples are `task/r3-factory-removal` and `task/r3-sql-tests`. Commit and push only that task's files:

   ```bash
   git add <changed-files>
   git commit -m "fix(data): make transfers atomic"
   git push
   ```

3. On GitHub, open the task PR with **base** `release/r3-di` and **compare** `task/r3-atomic-transactions`—not `main`. Another contributor reviews it; merge only after relevant checks pass.

4. If other tasks merge while a task remains open, update it from the release branch. This merge-based form avoids rebasing shared commits or force-pushing:

   ```bash
   git fetch origin
   git switch task/r3-atomic-transactions
   git merge origin/release/r3-di
   git push
   ```

5. After all task PRs merge and the release exit gate passes, open one final PR with **base** `main` and **compare** `release/r3-di`. After it merges, update local `main` and remove local/remote branches:

   ```bash
   git switch main
   git pull --ff-only origin main
   git branch -d release/r3-di
   git push origin --delete release/r3-di
   git branch -d task/r3-atomic-transactions
   git push origin --delete task/r3-atomic-transactions
   ```

   GitHub may already have deleted task branches automatically; a “remote ref does not exist” message is harmless in that case. Create `release/r4-validation` only from the newly updated `main`. Never create all release branches in advance, because later releases must include accepted earlier work.

## R0/R1 — decisions, baseline and buildable structure

| Subtask | Effort | Owner |
| --- | ---: | --- |
| Endpoint/permission matrix and behavior baseline | 2 h | TBD |
| Verify SQL dependencies and shared test access | 1 h | TBD |
| OAuth, staging and approval decisions | 1 h | TBD |
| Create solution, API host and test projects | 2.5 h | TBD |
| Build/run verification and handoff | 1.5 h | TBD |
| **Total** | **8 h** | |

1. From the `Home` menu and application controllers, make a short operation-to-endpoint matrix: input, success output, domain failures, SQL dependencies, intended caller and account-level permission. Include create, view/list, balance, recent transactions, deposit, withdraw, transfer and close. Agree on the first-release contract before parallel controller work.
2. Use the safe shared SQL environment to verify tables and stored procedures used by `AccountQueries` and `TransactionQueries`; record how contributors obtain **non-production** connection settings. Capture a few reproducible baseline examples (one for each account type and each money movement). No schema or seed scripts are tracked here, so do not assume a fresh database can be created from this repo.
3. Decide the real API consumers' OAuth client types, account-ownership rule and identity provider **now**; API validation is implemented in R6. If the optional R8 experiment is attempted, a console is an interactive **public client**: use a test token from a supported Authorization Code + PKCE/system-browser or Device Authorization flow, not an embedded client secret. Keycloak is a recommendation, not a decision. Confirm where staging runs and who can approve release and rollback.
4. Add a solution, ASP.NET Core .NET 8 API project and test project(s) without modifying the console `Main`. For example, from the repo root: `dotnet new sln -n GDB.App -f sln`, `dotnet new webapi -f net8.0 -n GDB.App.WebApi -o src/GDB.App.WebApi`, then `dotnet sln GDB.App.sln add src/GDB.App/GDB.App.csproj src/GDB.App.WebApi/GDB.App.WebApi.csproj`. Explicit `-f sln` avoids the `.slnx` default in newer SDKs. Add test projects once their framework is chosen. A minimal health endpoint should start without contacting SQL.
5. Verify `dotnet build GDB.App.sln` and `dotnet run --project src/GDB.App.WebApi/GDB.App.WebApi.csproj`; also build the original console project. Inspect `git status` after builds: `src/GDB.App/bin`, `obj` and `.vs` contain tracked artifacts and must not enter implementation PRs by accident.

**Handoff:** endpoint/permission matrix, provider decision owner and deadline, working API host, buildable console project, and DB access instructions that contain no credentials.

## R2 — adapt behavior and expose HTTP endpoints

| Subtask | Effort | Owner |
| --- | ---: | --- |
| Define API request/response DTOs and route mapping | 3 h | TBD |
| Add controllers and initial DI/adapters | 5 h | TBD |
| Complete scoped read/write endpoints | 5 h | TBD |
| Behavior tests and console comparison | 3 h | TBD |
| **Total** | **16 h** | |

1. Define API-only request and response DTOs for the agreed operations. Specify account identifiers, amounts, account types and response fields; never serialize `IAccount` directly, because domain entities include PIN-related state. Define route naming consistently and leave final `/api/v1` contract documentation for R5.
2. Add thin HTTP controllers that translate HTTP inputs to application calls and map outputs to DTOs. Initially register or adapt existing services so the API runs; do not force a simultaneous rewrite of every factory. Keep SQL operations on the safe DB and do not expose this incomplete API beyond restricted development/staging access.
3. Start with one read and one write end to end; then fill in create/view/list/close, balance/history and deposit/withdraw/transfer. Maintain an endpoint checklist rather than quietly dropping a console feature. Keep business decisions in Domain/Application, not controller actions.
4. Add focused tests for account rules and service behavior as functionality is wired. Compare representative SQL results and failure behavior with the console, recording any deliberate contract differences.

**Handoff:** all scoped endpoints can be exercised in a restricted environment, both hosts build, and remaining behavioral differences are written down. The endpoints are not yet approved for public traffic.

## R3 — complete dependency injection and repair persistence boundaries

| Subtask | Effort | Owner |
| --- | ---: | --- |
| Replace API-path factories/static dependencies | 3 h | TBD |
| Extract projects and correct dependency direction | 2 h | TBD |
| Make money movement and transaction logging atomic | 4 h | TBD |
| Add concurrency and rollback safeguards/tests | 4 h | TBD |
| Secure and validate DB configuration | 1 h | TBD |
| **Total** | **14 h** | |

1. Register interfaces and implementations in the API host. Replace `AccountRepositoryFactory.Create("DB")`, `TransactionRepositoryFactory.Create("DB")`, `DataBaseConnectionManager` and `AppLogger.CreateLogger<T>()` on the **API path** with injected repositories, a connection factory and `ILogger<T>`. Keep the console working; remove legacy wiring only when nothing needs it.
2. Move Domain, Application and Infrastructure into libraries in small buildable steps if the team keeps that split. Check project references: Infrastructure currently returns types from `Application/Dtos`; move or map shared data contracts so Application can depend on abstractions without a circular project reference. Build after each extraction.
3. For each deposit, withdrawal and transfer, trace the entire write sequence. Today balance updates and `SaveTransaction` use separate connections/commits. Refactor the operation to share a single connection and database transaction (or a database-side atomic operation), so balance changes **and** the transaction record commit or roll back together. Fail if expected rows are not updated.
4. Protect the read-modify-write step from lost updates under competing requests. Choose and document a SQL-supported approach after examining the available schema (for example, conditional update with checked row count or row-version concurrency). Test rollback when recording the transaction fails and test simultaneous withdrawals/transfers against the safe database. Do not rely on an in-memory repository test as proof of SQL atomicity.
5. Source API DB credentials from environment variables or user secrets; bind and validate non-secret settings at startup. Do not copy the console's committed `App.config` credentials into API configuration.

**Handoff:** API registrations are explicit, project references build cleanly, secrets are not committed, and SQL-backed tests demonstrate atomicity and the chosen concurrency behavior. If this gate fails, do not proceed toward an unrestricted release.

## R4 — models and validation

| Subtask | Effort | Owner |
| --- | ---: | --- |
| Define boundary and domain validation ownership | 2 h | TBD |
| Implement request validation and safe responses | 2 h | TBD |
| Cover account types and failure cases | 4 h | TBD |
| **Total** | **8 h** | |

1. Decide validation rules at the API boundary for required fields, account-number and PIN formats, positive amounts, transfer-to-self, account type and type-specific fields. Keep status, balance and withdrawal rules in Domain so validation in an HTTP controller cannot bypass them.
2. Use request DTO validation with `[ApiController]` (attributes or an explicit validator). Reject malformed data before DB writes. Do not echo PINs in response DTOs, OpenAPI examples, validation messages or logs.
3. Cover all four account types and both success and failure cases: inactive/missing account, duplicate account, wrong PIN, insufficient balance, invalid amount and transfer-to-self. Verify error cases do not change account balances or transaction history.

**Handoff:** requests are predictably validated; business-rule tests pass; intentional changes from console behavior are documented in the API contract.

## R5 — error handling, logging, middleware and versioned contract

| Subtask | Effort | Owner |
| --- | ---: | --- |
| Centralize problem responses and status mapping | 2 h | TBD |
| Add safe logging and `/api/v1` OpenAPI | 2 h | TBD |
| Add HTTP integration tests | 3 h | TBD |
| Configure CI and restricted staging smoke tests | 3 h | TBD |
| **Total** | **10 h** | |

1. Use a single exception-handling mechanism (for example, `UseExceptionHandler` with problem details). Map **specific** failures to agreed 400/404/409 responses rather than treating all `AccountException` values alike. Preserve 401 for missing/invalid authentication and 403 for insufficient permission once R6 is implemented; never expose stack traces.
2. Replace static API logging with structured `ILogger<T>` calls. Include useful correlation/request information but exclude PINs, tokens, connection strings and full account data. Add `/api/v1` routes and an OpenAPI v1 document. Do not add multiple API-version implementations before they are needed.
3. Add API integration tests with `WebApplicationFactory` for status codes, response shape and error handling. Make test service/database replacement explicit; run money-movement tests against the safe SQL environment rather than assuming a mock proves transaction semantics.
4. Create pull-request CI to restore, build and run fast tests. Run DB-dependent tests as a separately configured job or documented staging verification with securely provided settings. Smoke-test restricted staging using the first-release endpoint checklist.

**Handoff:** build and fast tests run automatically, SQL verification has a reproducible procedure, `/api/v1` contract and failures are documented, and sensitive details do not leak.

## R6 — OAuth authentication and account authorization

| Subtask | Effort | Owner |
| --- | ---: | --- |
| Integrate provider and validate bearer tokens | 3 h | TBD |
| Implement scopes/roles and account ownership | 4 h | TBD |
| Add authentication/authorization tests | 5 h | TBD |
| **Total** | **12 h** | |

1. Integrate the provider chosen in R0/R1 as an OAuth/OIDC token issuer. Configure ASP.NET Core bearer validation for issuer, signature, audience and expiration. The API validates access tokens; it does **not** issue its own tokens or treat a banking PIN as a login credential.
2. Configure only the flows the actual clients need: Authorization Code + PKCE for user-facing clients and Client Credentials for service clients. Give operations appropriate scopes/roles; ensure a caller cannot read, close or transfer from another account merely because their token is valid. Define how identity claims map to account ownership in the test database.
3. Add authorization tests for no token, malformed/expired token, insufficient scope, cross-account access and a permitted caller. Secure or disable interactive documentation in non-development environments. Re-run R5 HTTP tests with authentication enabled.

**Handoff:** all sensitive endpoints require the right permission **and** account access; tests prove unauthorized and cross-account calls fail. If provider setup or ownership rules are unresolved, deliver only a restricted staging/demo build and skip optional R8.

## R7 — Docker image and staging release check

| Subtask | Effort | Owner |
| --- | ---: | --- |
| Build hardened Web API image | 2 h | TBD |
| Document runtime configuration and prerequisites | 2 h | TBD |
| Run container security/data smoke tests and rollback check | 2 h | TBD |
| **Total** | **6 h** | |

1. Add a Web API container build that restores/publishes the .NET 8 project and runs its published output. Run as a non-root user where the base image permits. Keep connection strings and OAuth secrets out of build arguments and image layers; inject runtime configuration.
2. Document local build/run commands, port, health check, network access to SQL and the token issuer, and how a contributor loads non-production settings. Packaging the API in R7 does not prevent running Keycloak separately before R7 if it was chosen.
3. From the container, verify health, one authorized read, one successful money movement, a rejected unauthorized/cross-account call and SQL-backed transaction history. Record the image tag and how to redeploy the prior image; leave the console fallback until cutover approval.

**Handoff:** container smoke tests, R6 security gate and R3 data-integrity gate pass. Tag the API release according to `PLAN.md`, whether or not R8 is attempted. Production routing needs separate approval.

## R8 — optional, time-boxed console-to-API experiment

Start **only after R6/R7 pass** and only if up to four spare team-hours remain before October 12. Skip the experiment entirely if mandatory fixes need that time.

| Subtask | Effort | Owner |
| --- | ---: | --- |
| Add one read-only `HttpClient` menu path | 2 h | TBD |
| Configure safe test authentication and failure handling | 1 h | TBD |
| Demonstrate and record follow-up work | 1 h | TBD |
| **Maximum** | **4 h** | |

1. Pick **one read-only menu action**, such as view balance in `Presentation/UI/Home.cs`. Today `Home` constructs `AccountController` directly; replace that one path with a small `HttpClient` call to its authenticated `/api/v1` endpoint. The demonstration path is **console menu → HTTP client → Web API HTTP controller → application service → repository**; all other console paths remain unchanged.
2. Configure the API base URL outside source and supply a short-lived test user's bearer token through a safe local mechanism. If a token is not already available, obtain one using a provider-supported interactive public-client flow. Do not embed a client secret, commit a token or confuse the banking PIN with OAuth authentication. Display an API failure or timeout rather than a false success.
3. Demonstrate that one call against restricted staging, including an unauthorized case. Note what a future full migration would require: interactive login/token lifecycle, the remaining menu operations, error mapping, client tests and removal of direct SQL dependencies. **Do not remove `DataBaseProviderRegistration.Register()`** while other menu paths still use SQL.

**Handoff:** one authenticated console-to-API call works, or R8 is explicitly skipped. No full console migration, API release delay or timeline extension is implied.
