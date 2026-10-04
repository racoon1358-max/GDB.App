using GDB.App.Domain.Enums;
using GDB.App.Domain.Models;
using GDB.App.Infrastructure.Repositories.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data.Common;
using Xunit;

namespace GDB.R3.SqlTests;

// Requires a disposable, isolated SQL database with the console schema and stored procedures.
public sealed class MoneyMovementSqlTests
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("GDB_R3_TEST_CONNECTION");

    [Fact]
    public async Task FailedTransactionInsertRollsBackBalance()
    {
        if (ConnectionString is null) throw new InvalidOperationException("Set GDB_R3_TEST_CONNECTION to a disposable test database.");
        await using var fixture = await Fixture.CreateAsync(ConnectionString);
        var original = (await fixture.Accounts.GetAccountAsync(fixture.From))!.Balance;
        var originalTransactions = await fixture.CountTransactionsAsync();

        await Assert.ThrowsAnyAsync<DbException>(async () =>
        {
            await using var session = await fixture.Sessions.OpenAsync();
            var account = (await session.GetAccountForUpdateAsync(fixture.From))!;
            account.Deposit(25m);
            await session.SaveBalanceAsync(account);
            // Missing TransactionType lookup forces SQL INSERT failure after balance write.
            await session.SaveTransactionAsync(null, fixture.From, (TransactionType)9999, 25m, 0m, account.Balance);
            await session.CommitAsync();
        });

        Assert.Equal(original, (await fixture.Accounts.GetAccountAsync(fixture.From))!.Balance);
        Assert.Equal(originalTransactions, await fixture.CountTransactionsAsync());
    }

    [Fact]
    public async Task CompetingWithdrawalsAndTransfersPreserveBalances()
    {
        if (ConnectionString is null) throw new InvalidOperationException("Set GDB_R3_TEST_CONNECTION to a disposable test database.");
        await using var fixture = await Fixture.CreateAsync(ConnectionString);

        async Task Withdraw()
        {
            await using var session = await fixture.Sessions.OpenAsync();
            var account = (await session.GetAccountForUpdateAsync(fixture.From))!;
            account.Withdraw(100m, "1234");
            await session.SaveBalanceAsync(account);
            await session.SaveTransactionAsync(fixture.From, null, TransactionType.Withdraw, 100m, account.Balance, 0m);
            await session.CommitAsync();
        }

        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Withdraw()));
        Assert.Equal(500m, (await fixture.Accounts.GetAccountAsync(fixture.From))!.Balance);
        Assert.Equal(5, await fixture.CountTransactionsAsync(TransactionType.Withdraw));
        Assert.Equal(5, await fixture.CountTransactionsAsync());

        async Task Transfer()
        {
            await using var session = await fixture.Sessions.OpenAsync();
            var from = (await session.GetAccountForUpdateAsync(fixture.From))!;
            var to = (await session.GetAccountForUpdateAsync(fixture.To))!;
            from.Withdraw(10m, "1234");
            to.Deposit(10m);
            await session.SaveBalanceAsync(from);
            await session.SaveBalanceAsync(to);
            await session.SaveTransactionAsync(fixture.From, fixture.To, TransactionType.Transfer, 10m, from.Balance, to.Balance);
            await session.CommitAsync();
        }

        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Transfer()));
        Assert.Equal(450m, (await fixture.Accounts.GetAccountAsync(fixture.From))!.Balance);
        Assert.Equal(1050m, (await fixture.Accounts.GetAccountAsync(fixture.To))!.Balance);
        Assert.Equal(5, await fixture.CountTransactionsAsync(TransactionType.Transfer));
        Assert.Equal(10, await fixture.CountTransactionsAsync());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqlConnectionFactory _connections;
        public AccountRepositoryDB Accounts { get; }
        public SqlMoneyMovementSessionFactory Sessions { get; }
        public string From { get; } = "R3" + Guid.NewGuid().ToString("N")[..9];
        public string To { get; } = "R3" + Guid.NewGuid().ToString("N")[..9];

        private Fixture(string connectionString)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                { ["ConnectionStrings:GDBConnection"] = connectionString }).Build();
            _connections = new SqlConnectionFactory(config);
            Accounts = new AccountRepositoryDB(_connections, NullLogger<AccountRepositoryDB>.Instance);
            Sessions = new SqlMoneyMovementSessionFactory(_connections, Accounts);
        }

        public static async Task<Fixture> CreateAsync(string connectionString)
        {
            var fixture = new Fixture(connectionString);
            try
            {
                foreach (var number in new[] { fixture.From, fixture.To })
                    fixture.Accounts.SaveAccount(new CurrentAccount(number, "R3 SQL test", 30, 1000m,
                        AccountType.Current, AccountStatus.Active, "1234", AccountPrivilege.Silver), "1234");
                return fixture;
            }
            catch
            {
                await fixture.DisposeAsync();
                throw;
            }
        }

        public async Task<int> CountTransactionsAsync(TransactionType? type = null)
        {
            await using var connection = _connections.CreateConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) FROM Transactions AS t
                INNER JOIN TransactionTypes AS tt ON tt.TransactionTypeId = t.TransactionTypeId
                WHERE (t.FromAccountId IN (SELECT AccountId FROM Accounts WHERE AccountNumber IN (@From, @To))
                    OR t.ToAccountId IN (SELECT AccountId FROM Accounts WHERE AccountNumber IN (@From, @To)))
                    AND (@Type IS NULL OR tt.Code = @Type)";
            foreach (var (name, value) in new[]
            {
                ("@From", (object)From), ("@To", To),
                ("@Type", type is null ? DBNull.Value : type.Value.ToString().ToUpperInvariant())
            })
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value;
                command.Parameters.Add(parameter);
            }
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async ValueTask DisposeAsync()
        {
            await using var connection = _connections.CreateConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM Transactions WHERE FromAccountId IN (SELECT AccountId FROM Accounts WHERE AccountNumber IN (@From, @To))
                    OR ToAccountId IN (SELECT AccountId FROM Accounts WHERE AccountNumber IN (@From, @To));
                DELETE FROM CurrentAccounts WHERE AccountId IN (SELECT AccountId FROM Accounts WHERE AccountNumber IN (@From, @To));
                DELETE FROM Accounts WHERE AccountNumber IN (@From, @To);";
            foreach (var (name, value) in new[] { ("@From", From), ("@To", To) })
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value;
                command.Parameters.Add(parameter);
            }
            await command.ExecuteNonQueryAsync();
        }
    }
}
