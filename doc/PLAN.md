# Migration Plan

## Goal

Migrate the existing console application to an ASP.NET Core Web API while preserving its behavior, tests, and transaction semantics.

## 1. Planning and project assessment

1. **Review the existing project**
   - Inventory the solution, projects, source code, tests, database dependencies, and current build/run process.
   - Identify external consumers, deployment requirements, technical risks, and any undocumented behavior.
   - Record the current state as the baseline for migration and testing.

2. **Define the target outcome and success criteria**
   - Confirm the required API features and which console-app behavior must be preserved.
   - Agree on measurable completion criteria, including build, test, API behavior, security, and deployment expectations.
   - Identify what is explicitly out of scope for this migration.

3. **Decide the repository and ownership model**
   - Choose between a fork model and a single repository with multiple maintainers.
   - Confirm repository ownership, permissions, review requirements, and contribution responsibilities.

4. **Define repository versioning and Git workflow**
   - Choose the versioning strategy for the GitHub repository and releases.
   - Define the branch strategy, pull-request process, and release workflow.
   - Document how changes move from development through review, testing, and release.

5. **Prepare the effort estimate**
   - Break the migration into tasks covering architecture, implementation, database work, testing, CI, and rollout.
   - Estimate each task and identify dependencies, risks, and work that can happen in parallel.
   - Review the estimate with the people responsible for the work and update it as the project is assessed.

6. **Set the timeline**
   - Use the project assessment and effort estimate to set milestone dates.
   - Identify dependencies, review points, contingency time, and the final delivery deadline.
   - Revisit the timeline when scope, risks, or estimates change.

7. **Plan across dimensions**
   - Review the migration top-down across architecture, security, testing, operations, and rollout.
   - Ensure each implementation milestone has corresponding validation and release activities.

## 2. Migration execution

### Step 1: Move the existing project as a single unit

- Move the entire existing project into the new solution without splitting its code into separate projects.
- Preserve its existing structure and behavior at this stage.
- Confirm that the moved project builds and its existing tests pass.

### Step 2: Create and validate the ASP.NET Core Web API

- Create the ASP.NET Core Web API project targeting .NET 8.
- Set up minimal hosting and validate that the API starts.
- Add initial controllers that call the existing services.
- Keep the existing project intact until the API host is working.

### Step 3: Split code into libraries while preserving behavior

- Move domain models, enums, exceptions, and value objects into `GDB.App.Domain`.
- Move DTOs, services, and command classes into `GDB.App.Application`.
- Move repositories, database connection management, and queries into `GDB.App.Infrastructure`.
- Keep controllers in the Web API project.
- Preserve existing behavior and run the relevant tests after each move.

### Step 4: Introduce dependency injection and configuration

- Replace static singletons and hardcoded setup with dependency injection.
- Introduce an `IDbConnectionFactory` and inject it where database connections are required.
- Replace `AppLogger.CreateLogger<T>()` with `ILogger<T>` through constructor injection.
- Register application and infrastructure services in the API host.
- Store non-secret settings in configuration; keep credentials out of source control and use environment variables or a development secrets store.

### Step 5: Update database access for async and dependency injection

- Inject `IDbConnectionFactory` and `ILogger<AccountRepositoryDB>` into `AccountRepositoryDB`.
- Use asynchronous connection and command APIs where supported, passing cancellation tokens through public async methods.
- Preserve transaction behavior and use database transactions for related updates.
- Update repository contracts and implementations consistently, including nullable return values where appropriate.
- Verify transaction behavior and database results with tests.

### Step 6: Define the API surface and routing

- Add lightweight controllers that accept DTOs and call application services or commands.
- Define and document routes, including:
  - `POST /api/accounts`
  - `GET /api/accounts/{id}`
  - `POST /api/transactions/deposit`
  - `POST /api/transactions/withdraw`
- Use `[ApiController]` and validation attributes on request DTOs.

### Step 7: Add validation, error handling, and middleware

- Centralize exception-to-HTTP-status mapping using middleware or an exception handler.
- Map domain exceptions to appropriate status codes, such as not found, conflict, or bad request.
- Return a consistent error response containing a message and error code.
- Prevent stack traces and internal details from being returned to clients.

### Step 8: Preserve transaction semantics and atomicity

- Preserve database transactions for multi-row updates.
- Review read-modify-write operations for concurrency risks.
- Where needed, use optimistic concurrency or another database-supported concurrency strategy.
- Add tests that verify atomicity and expected behavior when operations fail.

### Step 9: Logging, configuration, and local development

- Use `ILogger<T>` and structured logging.
- Configure database settings through application configuration and environment-specific values.
- Keep secrets out of committed configuration files.
- Configure local launch settings and verify the API can be run locally.

### Step 10: Tests and CI

- Update unit-test references to target the appropriate class libraries.
- Keep existing service and command tests passing unless a behavior change is explicitly approved.
- Add API integration tests using `WebApplicationFactory<TEntryPoint>`.
- Update CI to build the libraries and Web API, run tests, and publish a Docker image if required.

### Step 11: Cutover and rollout

- Run the API locally and validate endpoints using Swagger.
- Deploy to staging and run smoke tests against the deployed API.
- Confirm monitoring, configuration, and rollback expectations.
- Once the API is validated, route consumers to it and retire the console application according to the agreed rollout plan.

### Step 12: Optional modernization

Evaluate these separately from the required migration scope:

- Use an interface-driven `IDbConnectionFactory` and remove the static database connection manager.
- Add cancellation tokens to public async APIs.
- Introduce typed configuration classes for database settings.
- Consider EF Core or Dapper if the benefits justify the added migration work; retaining ADO.NET is acceptable.
- Add health checks.

## 3. Release milestones

- [ ] **R0:** Complete project assessment, governance decisions, estimates, timeline, and solution structure.
- [ ] **R1:** Scaffold the ASP.NET Core Web API and validate basic host startup.
- [ ] **R2:** Move the entire existing project as a single unit, then wire it into the solution and API execution flow.
- [ ] **R3:** Introduce dependency injection and remove legacy factory-based wiring where appropriate.
- [ ] **R4:** Refactor models and validation logic for the API layer.
- [ ] **R5:** Add exception handling, logging, middleware, and API versioning.
- [ ] **R6:** Implement the agreed authorization and authentication requirements.
- [ ] **R7:** Containerize the application with Docker, if required by the deployment plan.
