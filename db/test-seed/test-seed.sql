-- GlobalDigitalBank — sample seed data
-- Run AFTER GDB_schema.sql and GDB_procedures.sql (the schema already seeds
-- the 5 lookup tables + AccountPrivilegeLimits). This script seeds 30 Accounts across all 4
-- subtypes, plus sample Transactions.
--
-- Lookup ids created by GDB_schema.sql (insert order = identity order):
--   AccountTypes:        1=SAVINGS  2=CURRENT  3=FIXED_DEPOSIT  4=SALARY
--   AccountStatuses:     1=ACTIVE   2=INACTIVE 3=SUSPENDED 4=CLOSED 5=FROZEN
--   AccountPrivileges:   1=PREMIUM  2=GOLD     3=SILVER
--   TransactionTypes:    1=DEPOSIT  2=WITHDRAW 3=TRANSFER
--   TransactionStatuses: 1=SUCCESS  2=PENDING  3=FAILURE
USE GDBDatabase;
GO
-- ========== Accounts (30 total: 10 Savings, 8 Current, 6 Fixed Deposit, 6 Salary) ==========
SET IDENTITY_INSERT Accounts ON;
INSERT INTO Accounts (AccountId, AccountNumber, Name, Age, AccountTypeId, Balance, AccountStatusId, AccountPrivilegeId, Pin) VALUES
-- Savings (1-10), AccountTypeId = 1
(1,  '1000001001', 'Aarav Sharma',    28, 1, 15000.00, 1, 3, '1111'),
(2,  '1000001002', 'Priya Iyer',      34, 1, 42000.50, 1, 2, '1112'),
(3,  '1000001003', 'Rohan Verma',     22, 1,  5000.00, 1, 3, '1113'),
(4,  '1000001004', 'Sneha Kapoor',    45, 1, 98000.00, 1, 1, '1114'),
(5,  '1000001005', 'Karan Mehta',     31, 1,  1200.00, 2, 3, '1115'),
(6,  '1000001006', 'Ananya Nair',     26, 1, 23000.75, 1, 3, '1116'),
(7,  '1000001007', 'Vikram Rao',      52, 1, 76000.00, 1, 2, '1117'),
(8,  '1000001008', 'Ishita Desai',    19, 1,  3000.00, 1, 3, '1118'),
(9,  '1000001009', 'Aditya Pillai',   38, 1, 61000.00, 1, 2, '1119'),
(10, '1000001010', 'Meera Joshi',     29, 1, 18500.25, 1, 3, '1120'),
-- Current (11-18), AccountTypeId = 2
(11, '1000002001', 'Rahul Gupta',     41, 2, 120000.00, 1, 1, '2111'),
(12, '1000002002', 'Divya Menon',     33, 2,  85000.00, 1, 2, '2112'),
(13, '1000002003', 'Siddharth Rao',   47, 2,   -5000.00, 1, 1, '2113'),
(14, '1000002004', 'Pooja Bhatt',     36, 2,  32000.00, 1, 3, '2114'),
(15, '1000002005', 'Manish Chawla',   50, 2,  99000.00, 5, 2, '2115'),
(16, '1000002006', 'Kavya Reddy',     27, 2,  15000.00, 1, 3, '2116'),
(17, '1000002007', 'Arjun Malhotra',  44, 2, 210000.00, 1, 1, '2117'),
(18, '1000002008', 'Neha Singh',      30, 2,  47000.00, 1, 2, '2118'),
-- Fixed Deposit (19-24), AccountTypeId = 3
(19, '1000003001', 'Sanjay Kulkarni', 55, 3, 500000.00, 1, 1, '3111'),
(20, '1000003002', 'Ritu Agarwal',    48, 3, 250000.00, 1, 2, '3112'),
(21, '1000003003', 'Deepak Chandra',  60, 3, 750000.00, 1, 1, '3113'),
(22, '1000003004', 'Nisha Thakur',    39, 3, 100000.00, 4, 3, '3114'),
(23, '1000003005', 'Amit Trivedi',    42, 3, 300000.00, 1, 2, '3115'),
(24, '1000003006', 'Swati Ranganathan', 35, 3, 150000.00, 1, 3, '3116'),
-- Salary (25-30), AccountTypeId = 4
(25, '1000004001', 'Varun Khanna',    26, 4, 45000.00, 1, 3, '4111'),
(26, '1000004002', 'Shreya Bose',     29, 4, 62000.00, 1, 2, '4112'),
(27, '1000004003', 'Nikhil Saxena',   33, 4, 38000.00, 1, 3, '4113'),
(28, '1000004004', 'Tanvi Pandey',    24, 4, 21000.00, 3, 3, '4114'),
(29, '1000004005', 'Harsh Vardhan',   37, 4, 71000.00, 1, 1, '4115'),
(30, '1000004006', 'Isha Kohli',      31, 4, 54000.00, 1, 2, '4116');
SET IDENTITY_INSERT Accounts OFF;
GO
-- ========== SavingsAccounts (AccountId 1-10) ==========
INSERT INTO SavingsAccounts (AccountId, InterestRate, MinimumBalance, WithdrawalLimit) VALUES
(1,  0.0350, 1000.00, 6),
(2,  0.0350, 1000.00, 6),
(3,  0.0400, 500.00,  4),
(4,  0.0350, 1000.00, 6),
(5,  0.0350, 1000.00, 6),
(6,  0.0375, 1000.00, 6),
(7,  0.0350, 2000.00, 8),
(8,  0.0400, 500.00,  4),
(9,  0.0350, 1000.00, 6),
(10, 0.0350, 1000.00, 6);
GO
-- ========== CurrentAccounts (AccountId 11-18) ==========
INSERT INTO CurrentAccounts (AccountId, OverdraftLimit, InterestRate, MinimumBalance) VALUES
(11, 25000.00, 0.00, 5000.00),
(12, 25000.00, 0.00, 5000.00),
(13, 50000.00, 0.00, 10000.00),
(14, 25000.00, 0.00, 5000.00),
(15, 25000.00, 0.00, 5000.00),
(16, 15000.00, 0.00, 2000.00),
(17, 100000.00, 0.00, 20000.00),
(18, 25000.00, 0.00, 5000.00);
GO
-- ========== FixedDepositAccounts (AccountId 19-24) ==========
INSERT INTO FixedDepositAccounts (AccountId, PrincipalAmount, InterestRate, StartDate, MaturityDate, MaturityAmount, TenureMonths, AutoRenew) VALUES
(19, 500000.00, 0.0650, '2025-01-15', '2026-01-15', 532500.00, 12, 1),
(20, 250000.00, 0.0650, '2025-03-01', '2025-09-01', 258125.00, 6,  0),
(21, 750000.00, 0.0700, '2024-11-10', '2026-11-10', 855000.00, 24, 1),
(22, 100000.00, 0.0625, '2025-05-20', '2026-05-20', 106250.00, 12, 0),
(23, 300000.00, 0.0650, '2025-02-01', '2025-08-01', 309750.00, 6,  0),
(24, 150000.00, 0.0680, '2025-06-01', '2026-06-01', 160200.00, 12, 1);
GO
-- ========== SalaryAccounts (AccountId 25-30) ==========
INSERT INTO SalaryAccounts (AccountId, EmployerName, EmployeeId, SalaryCreditDay, SalaryAmount, InactiveMonths) VALUES
(25, 'TechCorp',   'EMP1001', 1, 45000.00, 0),
(26, 'InnoSoft',   'EMP1002', 1, 62000.00, 0),
(27, 'DataWorks',  'EMP1003', 5, 38000.00, 1),
(28, 'CloudNine',  'EMP1004', 1, 21000.00, 3),
(29, 'TechCorp',   'EMP1005', 7, 71000.00, 0),
(30, 'FinEdge',    'EMP1006', 1, 54000.00, 0);
GO
-- ========== Transactions (37 sample rows: deposits, withdrawals, transfers) ==========
INSERT INTO Transactions (TransactionTypeId, FromAccountId, ToAccountId, Amount, TransactionStatusId, Timestamp, BalanceAfterFrom, BalanceAfterTo) VALUES
-- Deposits (TypeId=1: FromAccountId NULL)
(1, NULL, 1,  5000.00, 1, '2025-08-01 09:15:00', NULL, 15000.00),
(1, NULL, 4, 10000.00, 1, '2025-08-02 10:20:00', NULL, 98000.00),
(1, NULL, 11, 20000.00, 1, '2025-08-03 11:05:00', NULL, 120000.00),
(1, NULL, 19, 500000.00, 1, '2025-01-15 09:00:00', NULL, 500000.00),
(1, NULL, 25, 45000.00, 1, '2025-08-01 08:00:00', NULL, 45000.00),
(1, NULL, 26, 62000.00, 1, '2025-08-01 08:05:00', NULL, 62000.00),
(1, NULL, 7,  6000.00, 1, '2025-08-04 12:30:00', NULL, 76000.00),
(1, NULL, 17, 30000.00, 1, '2025-08-05 13:45:00', NULL, 210000.00),
-- Withdrawals (TypeId=2: ToAccountId NULL)
(2, 2,  NULL, 2000.00, 1, '2025-08-05 09:10:00', 42000.50, NULL),
(2, 6,  NULL, 1500.00, 1, '2025-08-06 10:00:00', 23000.75, NULL),
(2, 9,  NULL, 4000.00, 1, '2025-08-07 14:20:00', 61000.00, NULL),
(2, 12, NULL, 3000.00, 1, '2025-08-08 15:10:00', 85000.00, NULL),
(2, 14, NULL, 2500.00, 1, '2025-08-09 16:00:00', 32000.00, NULL),
(2, 27, NULL, 1000.00, 1, '2025-08-10 09:30:00', 38000.00, NULL),
(2, 5,  NULL,  500.00, 3, '2025-08-11 09:40:00', NULL, NULL),  -- FAILURE: insufficient/inactive
(2, 28, NULL, 5000.00, 3, '2025-08-12 10:15:00', NULL, NULL),  -- FAILURE: suspended account
-- Transfers (TypeId=3: both accounts set)
(3, 4,  1,  2000.00, 1, '2025-08-13 09:00:00', 96000.00, 17000.00),
(3, 7,  3,  1000.00, 1, '2025-08-14 09:20:00', 75000.00, 6000.00),
(3, 11, 12, 5000.00, 1, '2025-08-15 10:05:00', 115000.00, 90000.00),
(3, 17, 16, 8000.00, 1, '2025-08-16 11:15:00', 202000.00, 23000.00),
(3, 9,  10, 3000.00, 1, '2025-08-17 12:00:00', 58000.00, 21500.25),
(3, 20, 23, 10000.00, 1, '2025-08-18 13:30:00', 240000.00, 310000.00),
(3, 29, 30, 4000.00, 1, '2025-08-19 14:00:00', 67000.00, 58000.00),
(3, 25, 26, 1500.00, 1, '2025-08-20 15:20:00', 43500.00, 63500.00),
(3, 13, 18, 2000.00, 2, '2025-08-21 16:10:00', NULL, NULL),   -- PENDING
(3, 6,  8,  1000.00, 1, '2025-08-22 09:05:00', 22000.75, 4000.00),
(3, 21, 24, 20000.00, 1, '2025-08-23 09:45:00', 730000.00, 170000.00),
(3, 15, 11, 3000.00, 3, '2025-08-24 10:30:00', NULL, NULL),   -- FAILURE: frozen account
(3, 2,  9,  5000.00, 1, '2025-08-25 11:00:00', 37000.50, 66000.00),
(3, 19, 22, 15000.00, 1, '2025-08-26 12:20:00', 485000.00, 115000.00),
(3, 30, 27, 2500.00, 1, '2025-08-27 13:10:00', 51500.00, 40500.00),
(3, 16, 14, 1000.00, 1, '2025-08-28 14:40:00', 14000.00, 33000.00),
(3, 3,  5,  500.00, 1, '2025-08-29 15:15:00', 4500.00, 1700.00),
(3, 10, 1,  2000.00, 1, '2025-08-30 16:00:00', 19500.25, 17000.00),
(3, 24, 20, 5000.00, 1, '2025-08-31 09:10:00', 145000.00, 245000.00),
(3, 18, 13, 1500.00, 2, '2025-09-01 09:30:00', NULL, NULL),   -- PENDING
(3, 26, 25, 1000.00, 1, '2025-09-02 10:00:00', 62500.00, 44500.00);
GO
