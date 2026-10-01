using GDB.App.Application.Dtos;
using GDB.App.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using ApplicationAccountController = GDB.App.Application.Controllers.AccountController;

namespace GDB.WebApi.Controllers;

[ApiController]
[Route("api/accounts")]
public sealed class AccountController(ApplicationAccountController accountController)
    : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyCollection<ViewAllAccountsResponseDto>> GetAllAccounts() =>
        Ok(accountController.GetAllAccounts());

    [HttpGet("{accountNumber}/balance")]
    public async Task<ActionResult<ViewBalanceResponseDto>> GetBalance(string accountNumber)
    {
        try
        {
            return Ok(await accountController.GetBalanceAsync(accountNumber));
        }
        catch (AccountException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("{accountNumber}")]
    public async Task<ActionResult<ViewAccountResponseDto>> ViewAccount(string accountNumber)
    {
        try
        {
            return Ok(await accountController.ViewAccountAsync(accountNumber));
        }
        catch (AccountException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost]
    public ActionResult<CreateAccountResponseDto> CreateAccount(CreateAccountRequestDto request)
    {
        try
        {
            var account = accountController.CreateAccount(request);
            return CreatedAtAction(
                nameof(ViewAccount),
                new { accountNumber = account.AccountNumber },
                account);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (AccountException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{accountNumber}")]
    public async Task<ActionResult<CloseAccountResponseDto>> CloseAccount(string accountNumber)
    {
        try
        {
            var request = new CloseAccountRequestDto { AccountNumber = accountNumber };
            return Ok(await accountController.CloseAccountAsync(request));
        }
        catch (Exception exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
