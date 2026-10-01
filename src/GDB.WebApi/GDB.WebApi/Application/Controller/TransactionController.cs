using GDB.WebApi.Application.Models;
using GDB.WebApi.Application.Service.Implementation;
using Microsoft.AspNetCore.Mvc;

namespace GDB.WebApi.Application.Controller;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionController(TransactionService transactionService) : ControllerBase
{
    [HttpPost("deposit")]
    public ActionResult<TransactionResponse> Deposit(DepositRequest request) =>
        Execute(() => transactionService.Deposit(request));

    [HttpPost("withdraw")]
    public ActionResult<TransactionResponse> Withdraw(WithdrawRequest request) =>
        Execute(() => transactionService.Withdraw(request));

    [HttpPost("transfer")]
    public ActionResult<TransactionResponse> TransferFunds(TransferFundsRequest request) =>
        Execute(() => transactionService.TransferFunds(request));

    [HttpGet("{accountNumber}/recent")]
    public ActionResult<IReadOnlyCollection<TransactionResponse>> ViewRecentTransactions(
        string accountNumber)
    {
        try
        {
            return Ok(transactionService.ViewRecentTransactions(accountNumber));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    private ActionResult<TransactionResponse> Execute(Func<TransactionResponse> operation)
    {
        try
        {
            return Ok(operation());
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }
}
