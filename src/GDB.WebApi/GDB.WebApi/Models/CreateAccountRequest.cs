using GDB.App.Application.Dtos;
using GDB.App.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using BankAccountType = GDB.App.Domain.Enums.AccountType;

namespace GDB.WebApi.Models;

public sealed class CreateAccountRequest : IValidatableObject
{
    [Required, RegularExpression(@"^[0-9]{10}$")]
    public string? AccountNumber { get; set; }

    [Required, StringLength(100)]
    public string? Name { get; set; }

    [Range(18, 150)]
    public int Age { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Balance { get; set; }

    [Required, RegularExpression(@"^[0-9]{4}$")]
    public string? Pin { get; set; }

    [Required]
    public AccountType? AccountType { get; set; }

    [Required]
    public AccountStatus? Status { get; set; }

    [Required]
    public AccountPrivilege? Privilege { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? OverdraftLimit { get; set; }

    [Range(1, 1200)]
    public int? TenureMonths { get; set; }

    [Range(typeof(decimal), "0.01", "999.99")]
    public decimal? InterestRate { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? MinimumBalance { get; set; }

    [StringLength(100)]
    public string? EmployerName { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("Name is required.", [nameof(Name)]);

        if (Balance != decimal.Round(Balance, 2))
            yield return new ValidationResult("Balance must have at most two decimal places.", [nameof(Balance)]);

        if (AccountType is { } type && !Enum.IsDefined(type))
            yield return new ValidationResult("Invalid account type.", [nameof(AccountType)]);
        if (Status is { } status && !Enum.IsDefined(status))
            yield return new ValidationResult("Invalid account status.", [nameof(Status)]);
        if (Privilege is { } privilege && !Enum.IsDefined(privilege))
            yield return new ValidationResult("Invalid account privilege.", [nameof(Privilege)]);

        if (OverdraftLimit is { } overdraft && overdraft != decimal.Round(overdraft, 2))
            yield return new ValidationResult("Use at most two decimal places.", [nameof(OverdraftLimit)]);
        if (MinimumBalance is { } minimum && minimum != decimal.Round(minimum, 2))
            yield return new ValidationResult("Use at most two decimal places.", [nameof(MinimumBalance)]);
        if (InterestRate is { } rate && rate != decimal.Round(rate, 2))
            yield return new ValidationResult("Use at most two decimal places.", [nameof(InterestRate)]);

        if (AccountType == BankAccountType.Current && OverdraftLimit is null)
            yield return new ValidationResult("Overdraft limit is required.", [nameof(OverdraftLimit)]);
        if (AccountType == BankAccountType.Savings && MinimumBalance is null)
            yield return new ValidationResult("Minimum balance is required.", [nameof(MinimumBalance)]);
        if (AccountType is BankAccountType.Savings or BankAccountType.FixedDeposit && InterestRate is null)
            yield return new ValidationResult("Interest rate is required.", [nameof(InterestRate)]);
        if (AccountType == BankAccountType.FixedDeposit && TenureMonths is null)
            yield return new ValidationResult("Tenure is required.", [nameof(TenureMonths)]);
        if (AccountType == BankAccountType.Salary && string.IsNullOrWhiteSpace(EmployerName))
            yield return new ValidationResult("Employer name is required.", [nameof(EmployerName)]);
    }

    public CreateAccountRequestDto ToDto() => new()
    {
        AccountNumber = AccountNumber!,
        Name = Name!,
        Age = Age,
        Balance = Balance,
        Pin = Pin!,
        AccountType = AccountType!.Value,
        Status = Status!.Value,
        Privilege = Privilege!.Value,
        OverdraftLimit = OverdraftLimit ?? 0,
        TenureMonths = TenureMonths ?? 0,
        InterestRate = (double)(InterestRate ?? 0),
        MinimumBalance = MinimumBalance ?? 0,
        EmployerName = EmployerName ?? string.Empty
    };
}
