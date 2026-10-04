using System.ComponentModel.DataAnnotations;

namespace GDB.WebApi.Models;

public class DepositRequest : IValidatableObject
{
    [Required, RegularExpression(@"^[0-9]{10}$")]
    public string AccountNumber { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal Amount { get; init; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Amount != decimal.Round(Amount, 2))
            yield return new ValidationResult("Amount must have at most two decimal places.", [nameof(Amount)]);
    }
}

public sealed class WithdrawRequest : DepositRequest
{
    [Required, RegularExpression(@"^[0-9]{4}$")]
    public string Pin { get; init; } = string.Empty;
}

public sealed class TransferFundsRequest : IValidatableObject
{
    [Required, RegularExpression(@"^[0-9]{10}$")]
    public string FromAccountNumber { get; init; } = string.Empty;

    [Required, RegularExpression(@"^[0-9]{10}$")]
    public string ToAccountNumber { get; init; } = string.Empty;

    [Required, RegularExpression(@"^[0-9]{4}$")]
    public string Pin { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal Amount { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Amount != decimal.Round(Amount, 2))
            yield return new ValidationResult("Amount must have at most two decimal places.", [nameof(Amount)]);
        if (!string.IsNullOrEmpty(FromAccountNumber) && FromAccountNumber == ToAccountNumber)
            yield return new ValidationResult("Transfer accounts must differ.", [nameof(ToAccountNumber)]);
    }
}
