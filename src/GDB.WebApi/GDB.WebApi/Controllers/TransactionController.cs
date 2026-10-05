using GDB.App.Application.Dtos;
using GDB.App.Domain.Exceptions;
using GDB.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using ApplicationTransactionController = GDB.App.Application.Controllers.TransactionController;

namespace GDB.WebApi.Controllers;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionController(ApplicationTransactionController transactionController)
    : ControllerBase
{
    [HttpPost("deposit")]
    public async Task<ActionResult<DepositResponseDto>> Deposit(DepositRequest request) =>
        await Execute(() => transactionController.DepositAsync(
            request.AccountNumber,
            request.Amount));

    [HttpPost("withdraw")]
    public async Task<ActionResult<WithdrawResponseDto>> Withdraw(WithdrawRequest request) =>
        await Execute(() => transactionController.WithdrawAsync(
            request.AccountNumber,
            request.Pin,
            request.Amount));

    [HttpPost("transfer")]
    public async Task<ActionResult<TranferFundsResponseDto>> TransferFunds(
        TransferFundsRequest request) =>
        await Execute(() => transactionController.TransferFundsAsync(
            request.FromAccountNumber,
            request.ToAccountNumber,
            request.Pin,
            request.Amount));

    [HttpGet("{accountNumber}/recent")]
    public async Task<ActionResult<IReadOnlyCollection<ViewRecentTransactionsResponseDto>>>
        ViewRecentTransactions([FromRoute, RegularExpression(@"^[0-9]{10}$")] string accountNumber)
    {
        try
        {
            return Ok(await transactionController.GetRecentTransactionsAsync(accountNumber));
        }
        catch (AccountException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    private async Task<ActionResult<TResponse>> Execute<TResponse>(
        Func<Task<TResponse>> operation)
    {
        try
        {
            return Ok(await operation());
        }
        catch (AccountException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
