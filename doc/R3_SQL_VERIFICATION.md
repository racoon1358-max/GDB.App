# R3 SQL verification

API SQL configuration: provide `ConnectionStrings__GDBConnection` as an environment variable or `dotnet user-secrets` value (key `ConnectionStrings:GDBConnection`). No API `App.config` fallback exists. Startup rejects absent or incomplete SQL configuration. Keep SQL credentials outside source control. Console retains its existing configuration.

Money movement uses one SQL connection and transaction for locked account reads, domain checks, balance updates and the transaction record. `UPDLOCK, HOLDLOCK` on `Accounts` serializes competing changes until commit; transfer locks are acquired in ordinal account-number order. Each balance update and record insert must affect exactly one row. Disposing an uncommitted session rolls back all writes. This relies on the existing `Accounts`, subtype and transaction tables and their stored-procedure behavior; inspect the staging schema before approval.

Run SQL-backed checks only against an **isolated, disposable** SQL Server database with the console schema, lookup rows and stored procedures installed. Tests create uniquely named Current accounts, then delete their own transaction rows and accounts. Never point them at production. Set `GDB_R3_TEST_CONNECTION` outside the repository; run:

```sh
dotnet test tests/GDB.R3.SqlTests/GDB.R3.SqlTests.csproj
```

Tests fail without that variable, not silently skip. Rollback test forces a transaction-record insert failure after a balance update; concurrency test runs competing withdrawals and transfers. No database is provisioned by this repository. Record actual results against the team's safe SQL environment before marking R3 complete.
