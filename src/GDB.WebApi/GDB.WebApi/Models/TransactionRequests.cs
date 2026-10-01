using System.ComponentModel.DataAnnotations;

namespace GDB.WebApi.Models;

public class DepositRequest
{
    [Required, RegularExpression(@"^\d{10}$")]
    public string AccountNumber { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; init; }
}

public sealed class WithdrawRequest : DepositRequest
{
    [Required, RegularExpression(@"^\d{4}$")]
    public string Pin { get; init; } = string.Empty;
}

public sealed class TransferFundsRequest
{
    [Required, RegularExpression(@"^\d{10}$")]
    public string FromAccountNumber { get; init; } = string.Empty;

    [Required, RegularExpression(@"^\d{10}$")]
    public string ToAccountNumber { get; init; } = string.Empty;

    [Required, RegularExpression(@"^\d{4}$")]
    public string Pin { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; init; }
}
