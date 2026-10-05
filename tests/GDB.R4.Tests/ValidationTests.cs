using GDB.App.Application.Dtos;
using GDB.App.Domain;
using GDB.App.Domain.Enums;
using GDB.App.Domain.Exceptions;
using GDB.WebApi.Models;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace GDB.R4.Tests;

public class ValidationTests
{
    private static List<ValidationResult> Errors(object request)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), errors, true);
        return errors;
    }

    private static CreateAccountRequest ValidAccount(AccountType type = AccountType.Current) => new()
    {
        AccountNumber = "1234567890", Name = "Test", Age = 30, Balance = 2000,
        Pin = "1234", AccountType = type, Status = AccountStatus.Active,
        Privilege = AccountPrivilege.Silver, OverdraftLimit = 100,
        MinimumBalance = 1000, InterestRate = 4, TenureMonths = 12,
        EmployerName = "Test Employer"
    };

    [Theory]
    [InlineData(AccountType.Current)]
    [InlineData(AccountType.Savings)]
    [InlineData(AccountType.FixedDeposit)]
    [InlineData(AccountType.Salary)]
    public void AllFourAccountTypesHaveValidRequests(AccountType type)
    {
        var request = ValidAccount(type);
        Assert.Empty(Errors(request));
        Assert.Equal(type, request.ToDto().AccountType);
        Assert.DoesNotContain(typeof(CreateAccountResponseDto).GetProperties(), property => property.Name == "Pin");
    }

    [Fact]
    public void AccountNumberAndPinMustHaveCorrectFormat()
    {
        var request = ValidAccount();
        request.AccountNumber = "not an account";
        request.Pin = "abc4";
        Assert.NotEmpty(Errors(request));
    }

    [Fact]
    public void AccountCreationRejectsMissingAndInvalidValues()
    {
        var request = ValidAccount();
        request.AccountType = null;
        Assert.NotEmpty(Errors(request));

        request = ValidAccount();
        request.Age = 17;
        Assert.NotEmpty(Errors(request));

        request = ValidAccount();
        request.Balance = 10.001m;
        Assert.NotEmpty(Errors(request));

        request = ValidAccount();
        request.AccountType = (AccountType)999;
        Assert.NotEmpty(Errors(request));
    }

    [Fact]
    public void EachAccountTypeRequiresItsOwnFields()
    {
        var current = ValidAccount();
        current.OverdraftLimit = null;
        Assert.NotEmpty(Errors(current));

        var savings = ValidAccount(AccountType.Savings);
        savings.MinimumBalance = null;
        Assert.NotEmpty(Errors(savings));

        var fixedDeposit = ValidAccount(AccountType.FixedDeposit);
        fixedDeposit.TenureMonths = null;
        Assert.NotEmpty(Errors(fixedDeposit));

        var salary = ValidAccount(AccountType.Salary);
        salary.EmployerName = null;
        Assert.NotEmpty(Errors(salary));
    }

    [Fact]
    public void TransactionRequestsRejectBadAmountPinAndSelfTransfer()
    {
        Assert.NotEmpty(Errors(new DepositRequest { AccountNumber = "1234567890", Amount = 0 }));
        Assert.NotEmpty(Errors(new DepositRequest { AccountNumber = "1234567890", Amount = 1.001m }));
        Assert.NotEmpty(Errors(new WithdrawRequest { AccountNumber = "1234567890", Pin = "bad", Amount = 10 }));
        Assert.NotEmpty(Errors(new TransferFundsRequest
        {
            FromAccountNumber = "1234567890", ToAccountNumber = "1234567890", Pin = "1234", Amount = 10
        }));
        Assert.Empty(Errors(new DepositRequest { AccountNumber = "1234567890", Amount = 10 }));
    }

    [Theory]
    [InlineData(AccountType.Current)]
    [InlineData(AccountType.Savings)]
    [InlineData(AccountType.FixedDeposit)]
    [InlineData(AccountType.Salary)]
    public void DomainStillChecksPinAccountStateAndAmounts(AccountType type)
    {
        var active = AccountFactory.CreateAccount(type, "1234567890", "Test", 30, 2000,
            AccountStatus.Active, "1234", AccountPrivilege.Silver);
        Assert.Throws<InvalidPinException>(() => active.Withdraw(10, "0000"));
        active.Deposit(25);
        Assert.Equal(2025, active.Balance);
        Assert.Throws<InvalidAmountException>(() => active.Deposit(0));

        var inactive = AccountFactory.CreateAccount(type, "1234567890", "Test", 30, 2000,
            AccountStatus.Inactive, "1234", AccountPrivilege.Silver);
        Assert.Throws<InactiveAccountException>(() => inactive.Deposit(10));
        Assert.Equal(2000, inactive.Balance);
    }

    [Fact]
    public void WithdrawalLimitsStayInDomain()
    {
        var savings = AccountFactory.CreateAccount(AccountType.Savings, "1234567890", "Test", 30, 1000,
            AccountStatus.Active, "1234", AccountPrivilege.Silver);
        Assert.Throws<MinimumBalanceViolationException>(() => savings.Withdraw(10, "1234"));

        var salary = AccountFactory.CreateAccount(AccountType.Salary, "1234567891", "Test", 30, 1000,
            AccountStatus.Active, "1234", AccountPrivilege.Silver);
        Assert.Throws<InsufficientBalanceException>(() => salary.Withdraw(1001, "1234"));

        var fixedDeposit = AccountFactory.CreateAccount(AccountType.FixedDeposit, "1234567892", "Test", 30, 1000,
            AccountStatus.Active, "1234", AccountPrivilege.Silver);
        Assert.Throws<AccountException>(() => fixedDeposit.Withdraw(10, "1234"));
    }
}
