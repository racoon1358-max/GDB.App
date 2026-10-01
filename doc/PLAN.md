# Migration

## todo

1.  Versioning Plan (Github Repo)
    1. Branch Strategy
    1. Fork vs One repo with 3 Maintainers
1.  Effort Estimate > Tasks
1.  Timeline & Final Deadline
    1. Achieve Result
1.  Top Down : MultiDimension
1.  Release Plan:
    R0/R1: Plan action and create project structure folders.
    R2: Copy code, create controllers with dependency injection, and run the application.
    R3: Dependency Injection (DI) and removing factory patterns.
    R4: Models and Validation.
    R5: Exception handling, logging, middleware, and versioning.
    R6: Authorization and authentication
    R7: Docker Containerisation

Goal: Console app -> ASP.NET Web API

1. Create a new solution structure.

2. Create the ASP.NET Core Web API project
   - In Visual Studio, go to Add > New Project and choose ASP.NET Core Web API (.NET 8).
   - Replace the console Program/Main with minimal hosting in Program.cs.
   - Create controllers under Controllers/ that call the existing services.

3. Move code into libraries while preserving logic
   - Move domain models, enums, and exceptions unchanged into GDB.App.Domain.
   - Move service classes and commands such as Deposit/Withdraw/TransactionCommandFactory into GDB.App.Application.
   - Move AccountRepositoryDB, DataBaseConnectionManager, AccountQueries, and similar code into GDB.App.Infrastructure.

4. Use dependency injection and configuration
   - Replace static singletons and hardcoded setup with DI.
   - Create an IDbConnectionFactory in GDB.App.Infrastructure and inject it where needed.
   - Replace AppLogger.CreateLogger<T>() with ILogger<T> in constructors.
   - Register services in Program.cs and keep DB settings in appsettings.json.

5. Update DB code for async and DI
   - AccountRepositoryDB currently uses DataBaseConnectionManager.GetConnection() and many synchronous calls; change it to:
   - Accept IDbConnectionFactory and ILogger<AccountRepositoryDB> via the constructor.
   - Use using var conn = \_dbFactory.CreateConnection(); await conn.OpenAsync(cancellationToken) where applicable.
   - Use ExecuteNonQueryAsync, ExecuteReaderAsync, and ExecuteScalarAsync to avoid blocking the thread pool.
   - Keep transaction logic, but prefer DbTransaction from the connection and await async calls.
   - Example change to method signature: public async Task<IAccount?> GetAccountAsync(string accountNumber) returns nullable to match the repo contract.

6. Define controller surface and routing
   - Add lightweight controllers that accept DTOs and call service or command classes.
   - Example endpoints:
     - POST /api/accounts (create)
     - GET /api/accounts/{id}
     - POST /api/transactions/deposit
     - POST /api/transactions/withdraw
   - Use [ApiController] and model validation attributes on DTOs.

7. Add validation, error handling, and middleware
   - Centralize exception-to-status-code mapping using middleware or an exception filter.
   - Map domain exceptions:
     - AccountException -> 404 Bad Request (or 400)
     - InactiveAccountException -> 409 Conflict or 400
     - InvalidAmountException -> 400 Bad Request
   - Return consistent error DTOs (message, code).
   - Add UseExceptionHandler/custom middleware to prevent stack trace leakage.

8. Preserve transaction semantics and atomicity
   - Where multi-row updates (such as SaveAccounts with a transaction) are used, keep DB transactions.
   - Consider converting critical read-modify-write flows to a concurrency-safe pattern (optimistic concurrency, SQL row version) or at least ensure transactions are used.

9. Logging, configuration, and secrets
   - Use ILogger<T> and structured logging.
   - Move database credentials to appsettings.json and environment variables, and use IConfiguration.
   - Update Properties > launchSettings.json in the Web project for local debug ports.

10. Tests and CI
    - Update unit tests to reference class libraries instead of the console app project.
    - Add integration tests using WebApplicationFactory<TEntryPoint> (Microsoft.AspNetCore.Mvc.Testing) to validate endpoint behavior end-to-end.
    - Run all existing unit tests and add new API-level tests. Keep the original tests for service and command logic as-is; they should pass unchanged after refactor.
    - Update the CI pipeline to:
      - Build class libraries and the Web API.
      - Run tests.
      - Publish a Docker image if needed.

11. Gradual cutover and rollout
    - Run the Web API locally and test manually with Swagger (add builder.Services.AddSwaggerGen()).
    - Deploy to staging and run smoke tests against the endpoints.
    - Once validated, route consumers to the API and retire the console app.

12. Additional recommended improvements (opportunity to modernize)
    - Introduce an interface-driven IDbConnectionFactory and remove the static DataBaseConnectionManager.
    - Introduce cancellation tokens in public async APIs.
    - Use typed configuration classes for DB settings.
    - Consider EF Core or Dapper for more robust DB mapping (optional — keep the current ADO.NET code if minimal change is preferred).
    - Add health checks (builder.Services.AddHealthChecks()).
