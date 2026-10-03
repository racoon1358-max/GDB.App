using gdb.Logging;
using GDB.App.Application.Dtos;
using GDB.App.Application.Services.Contracts;
using GDB.App.Domain.Enums;
using GDB.App.Domain.Exceptions;
using GDB.App.Domain.Models;
using GDB.App.Infrastructure.Repositories.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDB.App.Application.Services.Implementations
{
    public class TransferTransactionCommand
    : ITransactionCommand<TranferFundsResponseDto>
    {
        private readonly IMoneyMovementSessionFactory _sessions;

        private readonly ILogger<TransferTransactionCommand> _logger;

        public TransferTransactionCommand(
            IMoneyMovementSessionFactory sessions,
            ILogger<TransferTransactionCommand> logger)
        {
            _sessions = sessions;
            _logger = logger;
        }

        public async Task<TranferFundsResponseDto> ExecuteAsync(
            TransactionDto transactionDto)
        {
            if (transactionDto.FromAccount == transactionDto.ToAccount)
                throw new AccountException("Cannot transfer to the same account.");

            await using var session = await _sessions.OpenAsync();
            // Stable lock order prevents opposite-direction transfers deadlocking.
            var first = string.CompareOrdinal(transactionDto.FromAccount, transactionDto.ToAccount) < 0
                ? transactionDto.FromAccount : transactionDto.ToAccount;
            var second = first == transactionDto.FromAccount
                ? transactionDto.ToAccount : transactionDto.FromAccount;
            var firstAccount = await session.GetAccountForUpdateAsync(first);
            var secondAccount = await session.GetAccountForUpdateAsync(second);
            IAccount fromAccount = first == transactionDto.FromAccount ? firstAccount : secondAccount;

            if (fromAccount == null)
            {
                _logger.LogWarning(
                    "Transfer failed: from account {AccountNumber} not found",
                    transactionDto.FromAccount);

                throw new AccountException(
                    "From account not found");
            }

            // Get receiver
            IAccount toAccount = first == transactionDto.ToAccount ? firstAccount : secondAccount;

            if (toAccount == null)
            {
                _logger.LogWarning(
                    "Transfer failed: to account {AccountNumber} not found",
                    transactionDto.ToAccount);

                throw new AccountException(
                    "To account not found");
            }

            // Check sender is active
            if (!fromAccount.CheckIfAccountIsActive())
            {
                throw new InactiveAccountException();
            }

            // Check receiver is active
            if (!toAccount.CheckIfAccountIsActive())
            {
                throw new InactiveAccountException();
            }

            // Check PIN
            if (!fromAccount.ValidatePin(
                    transactionDto.Pin))
            {
                throw new InvalidPinException();
            }

            // Withdraw from sender
            fromAccount.Withdraw(
                transactionDto.Amount,
                transactionDto.Pin);

            // Deposit into receiver
            toAccount.Deposit(
                transactionDto.Amount);

            // Save both accounts
            await session.SaveBalanceAsync(fromAccount);
            await session.SaveBalanceAsync(toAccount);

            // Save transaction
            await session.SaveTransactionAsync(
                transactionDto.FromAccount,
                transactionDto.ToAccount,
                TransactionType.Transfer,
                transactionDto.Amount,
                fromAccount.Balance,
                toAccount.Balance);
            await session.CommitAsync();

            _logger.LogInformation(
    "Transferred {Amount} from {FromAccount} to {ToAccount}",
    transactionDto.Amount.ToString("C", new CultureInfo("en-IN")),
    transactionDto.FromAccount,
    transactionDto.ToAccount);

            return new TranferFundsResponseDto
            {
                FromAccountNumber =
                    transactionDto.FromAccount,

                ToAccountNumber =
                    transactionDto.ToAccount,

                Amount =
                    transactionDto.Amount,

                FromAccountBalance =
                    fromAccount.Balance,

                ToAccountBalance =
                    toAccount.Balance,

                TransactionStat =
                    TransactionStatus.Success
            };
        }
    }
}
