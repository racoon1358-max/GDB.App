using GDB.App.Domain.Enums;
using GDB.App.Domain.Models;

namespace GDB.App.Infrastructure.Repositories.Contracts;

public interface IMoneyMovementSessionFactory
{
    Task<IMoneyMovementSession> OpenAsync();
}

public interface IMoneyMovementSession : IAsyncDisposable
{
    Task<IAccount?> GetAccountForUpdateAsync(string accountNumber);
    Task SaveBalanceAsync(IAccount account);
    Task SaveTransactionAsync(string? from, string? to, TransactionType type, decimal amount,
        decimal balanceAfterFrom, decimal balanceAfterTo);
    Task CommitAsync();
}
