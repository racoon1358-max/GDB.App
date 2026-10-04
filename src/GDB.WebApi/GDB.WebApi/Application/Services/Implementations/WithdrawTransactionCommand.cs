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
    public class WithdrawTransactionCommand
        : ITransactionCommand<WithdrawResponseDto>
    {
        private readonly IMoneyMovementSessionFactory _sessions;

        private readonly ILogger<WithdrawTransactionCommand> _logger;

        public WithdrawTransactionCommand(
            IMoneyMovementSessionFactory sessions,
            ILogger<WithdrawTransactionCommand> logger)
        {
            _sessions = sessions;
            _logger = logger;
        }

        public async Task<WithdrawResponseDto> ExecuteAsync(
            TransactionDto transactionDto)
        {
            await using var session = await _sessions.OpenAsync();
            IAccount account =
                await session.GetAccountForUpdateAsync(
                    transactionDto.AccountNumber);

            if (account == null)
            {
                _logger.LogWarning(
                    "Withdraw failed: account {AccountNumber} not found",
                    transactionDto.AccountNumber);

                throw new AccountException(
                    "Account not found");
            }

            // Domain handles PIN,
            // amount validation,
            // balance validation,
            // account-specific withdrawal rules.
            account.Withdraw(
                transactionDto.Amount,
                transactionDto.Pin);

            // Update balance
            await session.SaveBalanceAsync(account);

            // Save transaction
            await session.SaveTransactionAsync(
                transactionDto.AccountNumber,
                null,
                TransactionType.Withdraw,
                transactionDto.Amount,
                account.Balance,
                0);
            await session.CommitAsync();

            _logger.LogInformation(
    "Withdrew {Amount} from {AccountNumber}",
    transactionDto.Amount.ToString("C", new CultureInfo("en-IN")),
    transactionDto.AccountNumber);

            return new WithdrawResponseDto
            {
                Balance = account.Balance,
                TransactionStat = TransactionStatus.Success
            };
        }
    }
}
