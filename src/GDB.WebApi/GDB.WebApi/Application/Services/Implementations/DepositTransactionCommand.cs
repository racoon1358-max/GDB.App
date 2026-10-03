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
    public class DepositTransactionCommand
        : ITransactionCommand<DepositResponseDto>
    {
        private readonly IMoneyMovementSessionFactory _sessions;

        private readonly ILogger<DepositTransactionCommand> _logger;

        public DepositTransactionCommand(
            IMoneyMovementSessionFactory sessions,
            ILogger<DepositTransactionCommand> logger)
        {
            _sessions = sessions;
            _logger = logger;
        }

        public async Task<DepositResponseDto> ExecuteAsync(
            TransactionDto transactionDto)
        {
            await using var session = await _sessions.OpenAsync();
            IAccount account =
                await session.GetAccountForUpdateAsync(
                    transactionDto.AccountNumber);

            if (account == null)
            {
                _logger.LogWarning(
                    "Deposit failed: account {AccountNumber} not found",
                    transactionDto.AccountNumber);

                throw new AccountException(
                    "Account not found");
            }

            // Domain handles the actual deposit rules
            account.Deposit(transactionDto.Amount);

            // Update account balance
            await session.SaveBalanceAsync(account);

            // Save transaction record
            await session.SaveTransactionAsync(
                null,
                transactionDto.AccountNumber,
                TransactionType.Deposit,
                transactionDto.Amount,
                0,
                account.Balance);
            await session.CommitAsync();

            _logger.LogInformation(
    "Deposited {Amount} to {AccountNumber}",
    transactionDto.Amount.ToString("C", new CultureInfo("en-IN")),
    transactionDto.AccountNumber);

            return new DepositResponseDto
            {
                Balance = account.Balance,
                TransactionStat = TransactionStatus.Success
            };
        }
    }
}
