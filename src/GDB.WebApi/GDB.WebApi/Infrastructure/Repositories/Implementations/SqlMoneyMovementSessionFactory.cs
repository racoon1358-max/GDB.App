using GDB.App.Domain.Enums;
using GDB.App.Domain.Models;
using GDB.App.Infrastructure.Repositories.Contracts;
using GDB.App.Infrastructure.Repositories.Queries;
using System.Data;
using System.Data.Common;

namespace GDB.App.Infrastructure.Repositories.Implementations;

public sealed class SqlMoneyMovementSessionFactory(IDbConnectionFactory connections, AccountRepositoryDB accounts)
    : IMoneyMovementSessionFactory
{
    public async Task<IMoneyMovementSession> OpenAsync()
    {
        var connection = connections.CreateConnection();
        try
        {
            await connection.OpenAsync();
            var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            return new Session(connection, transaction, accounts);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private sealed class Session(DbConnection connection, DbTransaction transaction, AccountRepositoryDB accounts)
        : IMoneyMovementSession
    {
        private bool _committed;
        public async Task<IAccount?> GetAccountForUpdateAsync(string accountNumber)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = AccountQueries.GetAccountForUpdate;
            Add(command, "@AccountNumber", accountNumber);
            using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? accounts.CreateAccount(reader) : null;
        }

        public async Task SaveBalanceAsync(IAccount account)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = AccountQueries.UpdateBalance;
            Add(command, "@AccountNumber", account.AccountNumber);
            Add(command, "@Balance", account.Balance);
            if (await command.ExecuteNonQueryAsync() != 1)
                throw new DBConcurrencyException("Expected exactly one account balance update.");
        }

        public async Task SaveTransactionAsync(string? from, string? to, TransactionType type, decimal amount,
            decimal balanceAfterFrom, decimal balanceAfterTo)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = TransactionQueries.InsertTransaction;
            Add(command, "@FromAccountNumber", from);
            Add(command, "@ToAccountNumber", to);
            Add(command, "@TransactionType", type.ToString().ToUpperInvariant());
            Add(command, "@TransactionStatus", TransactionStatus.Success.ToString().ToUpperInvariant());
            Add(command, "@Amount", amount);
            Add(command, "@BalanceAfterFrom", balanceAfterFrom);
            Add(command, "@BalanceAfterTo", balanceAfterTo);
            if (await command.ExecuteNonQueryAsync() != 1)
                throw new DBConcurrencyException("Expected exactly one transaction record.");
        }

        public async Task CommitAsync()
        {
            await transaction.CommitAsync();
            _committed = true;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (!_committed)
                    await transaction.RollbackAsync();
            }
            finally
            {
                await transaction.DisposeAsync();
                await connection.DisposeAsync();
            }
        }

        private static void Add(DbCommand command, string name, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }
}
