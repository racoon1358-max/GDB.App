-- GlobalDigitalBank — database, tables and lookup data
-- Run on an isolated test SQL Server before GDB_procedures.sql and test-seed.sql.
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'GDBDatabase')
BEGIN
    CREATE DATABASE GDBDatabase;
END
GO
USE GDBDatabase;
GO
-- GlobalDigitalBank — SQL Server schema
-- Run in SSMS against your target database.
-- ========== Lookup tables (one per enum) ==========
CREATE TABLE AccountTypes (
    AccountTypeId   TINYINT      IDENTITY(1,1) PRIMARY KEY,
    Code            VARCHAR(20)  NOT NULL UNIQUE,   -- SAVINGS, CURRENT, FIXED_DEPOSIT, SALARY
    Description     VARCHAR(100) NULL
);
GO
CREATE TABLE AccountStatuses (
    AccountStatusId TINYINT      IDENTITY(1,1) PRIMARY KEY,
    Code            VARCHAR(20)  NOT NULL UNIQUE,   -- ACTIVE, INACTIVE, SUSPENDED, CLOSED, FROZEN
    Description     VARCHAR(100) NULL
);
GO
CREATE TABLE AccountPrivileges (
    AccountPrivilegeId TINYINT      IDENTITY(1,1) PRIMARY KEY,
    Code                VARCHAR(20)  NOT NULL UNIQUE,  -- PREMIUM, GOLD, SILVER
    Description          VARCHAR(100) NULL
);
GO
CREATE TABLE TransactionTypes (
    TransactionTypeId TINYINT      IDENTITY(1,1) PRIMARY KEY,
    Code               VARCHAR(20)  NOT NULL UNIQUE,   -- DEPOSIT, WITHDRAW, TRANSFER
    Description         VARCHAR(100) NULL
);
GO
CREATE TABLE TransactionStatuses (
    TransactionStatusId TINYINT      IDENTITY(1,1) PRIMARY KEY,
    Code                 VARCHAR(20)  NOT NULL UNIQUE,  -- SUCCESS, PENDING, FAILURE
    Description           VARCHAR(100) NULL
);
GO
-- ========== Master + subtype tables ==========
CREATE TABLE Accounts (
    AccountId           BIGINT IDENTITY(1,1) PRIMARY KEY,
    AccountNumber       VARCHAR(20)   NOT NULL UNIQUE,
    Name                VARCHAR(100)  NOT NULL,
    Age                 INT           NOT NULL CHECK (Age >= 18),
    AccountTypeId       TINYINT       NOT NULL REFERENCES AccountTypes(AccountTypeId),
    Balance             DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    AccountStatusId     TINYINT       NOT NULL REFERENCES AccountStatuses(AccountStatusId),
    AccountPrivilegeId  TINYINT       NOT NULL REFERENCES AccountPrivileges(AccountPrivilegeId),
    Pin                 CHAR(4)       NOT NULL
);
GO
CREATE TABLE SavingsAccounts (
    AccountId       BIGINT PRIMARY KEY REFERENCES Accounts(AccountId),
    InterestRate    DECIMAL(5,4)  NOT NULL DEFAULT 0.0350,
    MinimumBalance  DECIMAL(18,2) NOT NULL DEFAULT 1000.00,
    WithdrawalLimit INT           NOT NULL DEFAULT 6
);
GO
CREATE TABLE CurrentAccounts (
    AccountId       BIGINT PRIMARY KEY REFERENCES Accounts(AccountId),
    OverdraftLimit  DECIMAL(18,2) NOT NULL DEFAULT 25000.00,
    InterestRate    DECIMAL(5,4)  NOT NULL DEFAULT 0.0,
    MinimumBalance  DECIMAL(18,2) NOT NULL DEFAULT 0.00
);
GO
CREATE TABLE FixedDepositAccounts (
    AccountId       BIGINT PRIMARY KEY REFERENCES Accounts(AccountId),
    PrincipalAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    InterestRate    DECIMAL(5,4)  NOT NULL DEFAULT 0.0650,
    StartDate       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    MaturityDate    DATETIME2     NOT NULL,
    MaturityAmount  DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    TenureMonths    INT           NOT NULL DEFAULT 12,
    AutoRenew       BIT           NOT NULL DEFAULT 0
);
GO
CREATE TABLE SalaryAccounts (
    AccountId        BIGINT PRIMARY KEY REFERENCES Accounts(AccountId),
    EmployerName     VARCHAR(100)  NOT NULL DEFAULT 'TechCorp',
    EmployeeId       VARCHAR(64)   NULL,
    SalaryCreditDay  INT           NOT NULL DEFAULT 1,
    SalaryAmount     DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    InactiveMonths   INT           NOT NULL DEFAULT 0
);
GO
CREATE TABLE AccountPrivilegeLimits (
    AccountPrivilegeId TINYINT PRIMARY KEY REFERENCES AccountPrivileges(AccountPrivilegeId),
    DailyLimit          DECIMAL(18,2) NOT NULL,
    TotalTransactions   INT NOT NULL DEFAULT 0
);
GO
CREATE TABLE Transactions (
    TransactionId        BIGINT IDENTITY(1,1) PRIMARY KEY,
    TransactionTypeId    TINYINT       NOT NULL REFERENCES TransactionTypes(TransactionTypeId),
    FromAccountId        BIGINT NULL REFERENCES Accounts(AccountId),
    ToAccountId          BIGINT NULL REFERENCES Accounts(AccountId),
    Amount               DECIMAL(18,2) NOT NULL,
    TransactionStatusId  TINYINT       NOT NULL REFERENCES TransactionStatuses(TransactionStatusId),
    Timestamp             DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    BalanceAfterFrom      DECIMAL(18,2) NULL,
    BalanceAfterTo        DECIMAL(18,2) NULL
);
GO
-- ========== Seed lookup data ==========
INSERT INTO AccountTypes (Code) VALUES ('SAVINGS'), ('CURRENT'), ('FIXED_DEPOSIT'), ('SALARY');
INSERT INTO AccountStatuses (Code) VALUES ('ACTIVE'), ('INACTIVE'), ('SUSPENDED'), ('CLOSED'), ('FROZEN');
INSERT INTO AccountPrivileges (Code) VALUES ('PREMIUM'), ('GOLD'), ('SILVER');
INSERT INTO TransactionTypes (Code) VALUES ('DEPOSIT'), ('WITHDRAW'), ('TRANSFER');
INSERT INTO TransactionStatuses (Code) VALUES ('SUCCESS'), ('PENDING'), ('FAILURE');
GO
INSERT INTO AccountPrivilegeLimits (AccountPrivilegeId, DailyLimit)
SELECT AccountPrivilegeId, CASE Code
    WHEN 'PREMIUM' THEN 100000
    WHEN 'GOLD'    THEN 50000
    WHEN 'SILVER'  THEN 25000
END
FROM AccountPrivileges;
GO
