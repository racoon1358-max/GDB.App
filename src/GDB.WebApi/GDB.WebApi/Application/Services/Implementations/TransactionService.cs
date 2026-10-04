using GDB.App.Application.Dtos;
using GDB.App.Application.Services.Contracts;
using GDB.App.Domain.Enums;
using gdb.Logging;
using Microsoft.Extensions.Logging;

namespace GDB.App.Application.Services.Implementations
{
    public class TransactionService : ITransactionService
    {
        private readonly ILogger<TransactionService> _logger;
        private readonly DepositTransactionCommand _deposit;
        private readonly WithdrawTransactionCommand _withdraw;
        private readonly TransferTransactionCommand _transfer;

        public TransactionService(ILogger<TransactionService> logger, DepositTransactionCommand deposit,
            WithdrawTransactionCommand withdraw, TransferTransactionCommand transfer)
        {
            _logger = logger;
            _deposit = deposit;
            _withdraw = withdraw;
            _transfer = transfer;
        }

        public async Task<TResponse> ProcessTransactionAsync<TResponse>(
            TransactionDto transactionDto,
            TransactionType transactionType)
        {
            _logger.LogInformation(
                "Processing transaction {TransactionType}",
                transactionType);

            object response = transactionType switch
            {
                TransactionType.Deposit => await _deposit.ExecuteAsync(transactionDto),
                TransactionType.Withdraw => await _withdraw.ExecuteAsync(transactionDto),
                TransactionType.Transfer => await _transfer.ExecuteAsync(transactionDto),
                _ => throw new ArgumentOutOfRangeException(nameof(transactionType))
            };

            _logger.LogInformation(
                "Transaction {TransactionType} completed successfully",
                transactionType);

            return (TResponse)response;
        }
    }
}
