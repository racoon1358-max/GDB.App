using GDB.WebApi.Application.Models;
using GDB.WebApi.Application.Service.Implementation;
using Microsoft.AspNetCore.Mvc;

namespace GDB.WebApi.Application.Controller;

[ApiController]
[Route("api/accounts")]
public sealed class AccountController(AccountService accountService) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyCollection<AccountResponse>> GetAllAccounts() =>
        Ok(accountService.GetAllAccounts());

    [HttpGet("{accountNumber}/balance")]
    public ActionResult<BalanceResponse> GetBalance(string accountNumber)
    {
        try
        {
            return Ok(accountService.GetBalance(accountNumber));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("{accountNumber}")]
    public ActionResult<AccountResponse> ViewAccount(string accountNumber)
    {
        try
        {
            return Ok(accountService.ViewAccount(accountNumber));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost]
    public ActionResult<AccountResponse> CreateAccount(CreateAccountRequest request)
    {
        try
        {
            var account = accountService.CreateAccount(request);
            return CreatedAtAction(
                nameof(ViewAccount),
                new { accountNumber = account.AccountNumber },
                account);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpDelete("{accountNumber}")]
    public ActionResult<AccountResponse> CloseAccount(string accountNumber)
    {
        try
        {
            return Ok(accountService.CloseAccount(accountNumber));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}
