using GDB.App.Application.Dtos;
using GDB.App.Domain.Exceptions;
using GDB.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
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
    public async Task<ActionResult<ViewBalanceResponseDto>> GetBalance(
        [FromRoute, RegularExpression(@"^[0-9]{10}$")] string accountNumber)
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
    public async Task<ActionResult<ViewAccountResponseDto>> ViewAccount(
        [FromRoute, RegularExpression(@"^[0-9]{10}$")] string accountNumber)
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
    public ActionResult<CreateAccountResponseDto> CreateAccount(CreateAccountRequest request)
    {
        try
        {
            var account = accountController.CreateAccount(request.ToDto());
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
    public async Task<ActionResult<CloseAccountResponseDto>> CloseAccount(
        [FromRoute, RegularExpression(@"^[0-9]{10}$")] string accountNumber)
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
