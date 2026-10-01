using GDB.WebApi.Application.Models;

namespace GDB.WebApi.Application.Service.Implementation;

public sealed class AccountService
{
    private readonly object _sync = new();
    private readonly Dictionary<string, Account> _accounts = new()
    {
        ["1000000001"] = new("1000000001", "Asha", 10_000m, "1234"),
        ["1000000002"] = new("1000000002", "Ravi", 7_500m, "5678")
    };

    public IReadOnlyCollection<AccountResponse> GetAllAccounts()
    {
        lock (_sync)
        {
            return _accounts.Values.Select(ToResponse).ToArray();
        }
    }

    public BalanceResponse GetBalance(string accountNumber)
    {
        lock (_sync)
        {
            var account = GetRequiredAccount(accountNumber);
            return new BalanceResponse(account.AccountNumber, account.Balance);
        }
    }

    public AccountResponse ViewAccount(string accountNumber)
    {
        lock (_sync)
        {
            return ToResponse(GetRequiredAccount(accountNumber));
        }
    }

    public AccountResponse CreateAccount(CreateAccountRequest request)
    {
        lock (_sync)
        {
            if (_accounts.ContainsKey(request.AccountNumber))
            {
                throw new InvalidOperationException("An account with this number already exists.");
            }

            var account = new Account(
                request.AccountNumber,
                request.Name,
                request.InitialBalance,
                request.Pin);

            _accounts.Add(account.AccountNumber, account);
            return ToResponse(account);
        }
    }

    public AccountResponse CloseAccount(string accountNumber)
    {
        lock (_sync)
        {
            var account = GetRequiredAccount(accountNumber);
            account.Status = "Closed";
            return ToResponse(account);
        }
    }

    public decimal Deposit(string accountNumber, decimal amount)
    {
        lock (_sync)
        {
            var account = GetActiveAccount(accountNumber);
            account.Balance += amount;
            return account.Balance;
        }
    }

    public decimal Withdraw(string accountNumber, string pin, decimal amount)
    {
        lock (_sync)
        {
            var account = GetActiveAccount(accountNumber);
            ValidatePin(account, pin);

            if (account.Balance < amount)
            {
                throw new InvalidOperationException("The account has insufficient funds.");
            }

            account.Balance -= amount;
            return account.Balance;
        }
    }

    public void Transfer(string fromAccountNumber, string toAccountNumber, string pin, decimal amount)
    {
        lock (_sync)
        {
            if (fromAccountNumber == toAccountNumber)
            {
                throw new InvalidOperationException("Source and destination accounts must be different.");
            }

            var source = GetActiveAccount(fromAccountNumber);
            var destination = GetActiveAccount(toAccountNumber);
            ValidatePin(source, pin);

            if (source.Balance < amount)
            {
                throw new InvalidOperationException("The source account has insufficient funds.");
            }

            source.Balance -= amount;
            destination.Balance += amount;
        }
    }

    private Account GetActiveAccount(string accountNumber)
    {
        var account = GetRequiredAccount(accountNumber);
        if (account.Status != "Active")
        {
            throw new InvalidOperationException("The account is not active.");
        }

        return account;
    }

    private Account GetRequiredAccount(string accountNumber) =>
        _accounts.TryGetValue(accountNumber, out var account)
            ? account
            : throw new KeyNotFoundException("Account not found.");

    private static void ValidatePin(Account account, string pin)
    {
        if (account.Pin != pin)
        {
            throw new UnauthorizedAccessException("Invalid PIN.");
        }
    }

    private static AccountResponse ToResponse(Account account) =>
        new(account.AccountNumber, account.Name, account.Balance, account.Status);

    private sealed class Account(string accountNumber, string name, decimal balance, string pin)
    {
        public string AccountNumber { get; } = accountNumber;
        public string Name { get; } = name;
        public decimal Balance { get; set; } = balance;
        public string Pin { get; } = pin;
        public string Status { get; set; } = "Active";
    }
}
