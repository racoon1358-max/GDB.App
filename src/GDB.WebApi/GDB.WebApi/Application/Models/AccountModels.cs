using System.ComponentModel.DataAnnotations;

namespace GDB.WebApi.Application.Models;

public sealed record AccountResponse(
    string AccountNumber,
    string Name,
    decimal Balance,
    string Status);

public sealed record BalanceResponse(
    string AccountNumber,
    decimal Balance);

public sealed class CreateAccountRequest
{
    [Required, RegularExpression(@"^\d{10}$")]
    public string AccountNumber { get; init; } = string.Empty;

    [Required]
    public string Name { get; init; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal InitialBalance { get; init; }

    [Required, RegularExpression(@"^\d{4}$")]
    public string Pin { get; init; } = string.Empty;
}
