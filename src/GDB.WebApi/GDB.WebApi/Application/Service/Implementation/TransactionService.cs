using GDB.WebApi.Application.Models;

namespace GDB.WebApi.Application.Service.Implementation;

public sealed class TransactionService(AccountService accountService)
{
    private readonly object _sync = new();
    private readonly List<TransactionResponse> _transactions = [];

    public TransactionResponse Deposit(DepositRequest request)
    {
        accountService.Deposit(request.AccountNumber, request.Amount);
        return Record("Deposit", null, request.AccountNumber, request.Amount);
    }

    public TransactionResponse Withdraw(WithdrawRequest request)
    {
        accountService.Withdraw(request.AccountNumber, request.Pin, request.Amount);
        return Record("Withdraw", request.AccountNumber, null, request.Amount);
    }

    public TransactionResponse TransferFunds(TransferFundsRequest request)
    {
        accountService.Transfer(
            request.FromAccountNumber,
            request.ToAccountNumber,
            request.Pin,
            request.Amount);

        return Record(
            "Transfer",
            request.FromAccountNumber,
            request.ToAccountNumber,
            request.Amount);
    }

    public IReadOnlyCollection<TransactionResponse> ViewRecentTransactions(string accountNumber)
    {
        // Confirm the account exists before returning its history.
        accountService.ViewAccount(accountNumber);

        lock (_sync)
        {
            return _transactions
                .Where(transaction =>
                    transaction.FromAccountNumber == accountNumber ||
                    transaction.ToAccountNumber == accountNumber)
                .OrderByDescending(transaction => transaction.Timestamp)
                .Take(10)
                .ToArray();
        }
    }

    private TransactionResponse Record(
        string type,
        string? fromAccountNumber,
        string? toAccountNumber,
        decimal amount)
    {
        var transaction = new TransactionResponse(
            Guid.NewGuid(),
            type,
            fromAccountNumber,
            toAccountNumber,
            amount,
            DateTimeOffset.UtcNow);

        lock (_sync)
        {
            _transactions.Add(transaction);
        }

        return transaction;
    }
}
