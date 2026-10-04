-- Run after GDB_schema.sql against a disposable SQL Server database.
-- The caller owns the transaction spanning CreateAccount and its subtype insert.
USE GDBDatabase;
GO

CREATE OR ALTER PROCEDURE dbo.GetAccount
    @AccountNumber VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.AccountId,
        a.AccountNumber,
        a.Name,
        a.Age,
        a.Balance,
        a.Pin,
        at.Code AS AccountType,
        ast.Code AS AccountStatus,
        ap.Code AS AccountPrivilege,
        sa.InterestRate AS SavingsInterestRate,
        sa.MinimumBalance AS SavingsMinimumBalance,
        ca.OverdraftLimit,
        fda.InterestRate AS FixedDepositInterestRate,
        fda.TenureMonths,
        sya.EmployerName,
        sya.InactiveMonths
    FROM dbo.Accounts AS a
    INNER JOIN dbo.AccountTypes AS at ON at.AccountTypeId = a.AccountTypeId
    INNER JOIN dbo.AccountStatuses AS ast ON ast.AccountStatusId = a.AccountStatusId
    INNER JOIN dbo.AccountPrivileges AS ap ON ap.AccountPrivilegeId = a.AccountPrivilegeId
    LEFT JOIN dbo.SavingsAccounts AS sa ON sa.AccountId = a.AccountId
    LEFT JOIN dbo.CurrentAccounts AS ca ON ca.AccountId = a.AccountId
    LEFT JOIN dbo.FixedDepositAccounts AS fda ON fda.AccountId = a.AccountId
    LEFT JOIN dbo.SalaryAccounts AS sya ON sya.AccountId = a.AccountId
    WHERE a.AccountNumber = @AccountNumber;
END;
GO

CREATE OR ALTER PROCEDURE dbo.CreateAccount
    @AccountNumber VARCHAR(20),
    @Name VARCHAR(100),
    @Age INT,
    @AccountType VARCHAR(20),
    @Balance DECIMAL(18,2),
    @AccountStatus VARCHAR(20),
    @AccountPrivilege VARCHAR(20),
    @Pin CHAR(4)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AccountTypeId TINYINT =
        (SELECT AccountTypeId FROM dbo.AccountTypes WHERE Code = @AccountType);
    DECLARE @AccountStatusId TINYINT =
        (SELECT AccountStatusId FROM dbo.AccountStatuses WHERE Code = @AccountStatus);
    DECLARE @AccountPrivilegeId TINYINT =
        (SELECT AccountPrivilegeId FROM dbo.AccountPrivileges WHERE Code = @AccountPrivilege);

    IF @AccountTypeId IS NULL OR @AccountStatusId IS NULL OR @AccountPrivilegeId IS NULL
        THROW 50001, 'Invalid account type, status, or privilege.', 1;

    INSERT INTO dbo.Accounts
        (AccountNumber, Name, Age, AccountTypeId, Balance, AccountStatusId, AccountPrivilegeId, Pin)
    VALUES
        (@AccountNumber, @Name, @Age, @AccountTypeId, @Balance, @AccountStatusId, @AccountPrivilegeId, @Pin);

    -- ExecuteScalar expects one BIGINT result. Do not commit the caller's transaction here.
    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS AccountId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.InsertSavingsAccount
    @AccountId BIGINT,
    @InterestRate DECIMAL(5,4),
    @MinimumBalance DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.SavingsAccounts (AccountId, InterestRate, MinimumBalance)
    VALUES (@AccountId, @InterestRate, @MinimumBalance);
END;
GO

CREATE OR ALTER PROCEDURE dbo.InsertCurrentAccount
    @AccountId BIGINT,
    @OverdraftLimit DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.CurrentAccounts (AccountId, OverdraftLimit)
    VALUES (@AccountId, @OverdraftLimit);
END;
GO

CREATE OR ALTER PROCEDURE dbo.InsertFixedDepositAccount
    @AccountId BIGINT,
    @InterestRate DECIMAL(5,4),
    @TenureMonths INT,
    @PrincipalAmount DECIMAL(18,2),
    @MaturityAmount DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.FixedDepositAccounts
        (AccountId, InterestRate, TenureMonths, PrincipalAmount, MaturityDate, MaturityAmount)
    VALUES
        (@AccountId, @InterestRate, @TenureMonths, @PrincipalAmount,
         DATEADD(MONTH, @TenureMonths, SYSUTCDATETIME()), @MaturityAmount);
END;
GO

CREATE OR ALTER PROCEDURE dbo.InsertSalaryAccount
    @AccountId BIGINT,
    @EmployerName VARCHAR(100),
    @InactiveMonths INT,
    @SalaryAmount DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.SalaryAccounts (AccountId, EmployerName, InactiveMonths, SalaryAmount)
    VALUES (@AccountId, @EmployerName, @InactiveMonths, @SalaryAmount);
END;
GO
