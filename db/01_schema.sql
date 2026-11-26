/* AcuPay - 01_schema.sql : tables, keys, constraints, indexes. Safe to run on an empty server. */
IF DB_ID(N'AcuPay') IS NULL CREATE DATABASE AcuPay;
GO
USE AcuPay;
GO

CREATE TABLE dbo.Users (
    UserId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Username     NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
    PasswordHash NVARCHAR(200) NOT NULL,
    Role         NVARCHAR(20)  NOT NULL CONSTRAINT CK_Users_Role CHECK (Role IN ('Requester','Approver','Finance'))
);
GO

CREATE TABLE dbo.Vendors (
    VendorId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Vendors PRIMARY KEY,
    VendorCode NVARCHAR(20)  NOT NULL CONSTRAINT UQ_Vendors_VendorCode UNIQUE,
    VendorName NVARCHAR(200) NOT NULL,
    IsActive   BIT NOT NULL CONSTRAINT DF_Vendors_IsActive DEFAULT 1
);
GO

CREATE TABLE dbo.PurchaseOrders (
    PoId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseOrders PRIMARY KEY,
    PoNumber NVARCHAR(30) NOT NULL CONSTRAINT UQ_PurchaseOrders_PoNumber UNIQUE,
    VendorId INT NOT NULL CONSTRAINT FK_PurchaseOrders_Vendors REFERENCES dbo.Vendors(VendorId),
    PoAmount DECIMAL(18,2) NOT NULL CONSTRAINT CK_PurchaseOrders_PoAmount CHECK (PoAmount > 0),
    PoDate   DATE NOT NULL,
    Status   NVARCHAR(20) NOT NULL CONSTRAINT DF_PurchaseOrders_Status DEFAULT 'Open'
             CONSTRAINT CK_PurchaseOrders_Status CHECK (Status IN ('Open','Closed'))
);
GO

CREATE TABLE dbo.Invoices (
    InvoiceId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Invoices PRIMARY KEY,
    InvoiceNumber  NVARCHAR(50) NOT NULL,
    VendorId       INT NOT NULL CONSTRAINT FK_Invoices_Vendors REFERENCES dbo.Vendors(VendorId),
    PoId           INT NULL     CONSTRAINT FK_Invoices_PurchaseOrders REFERENCES dbo.PurchaseOrders(PoId),
    InvoiceAmount  DECIMAL(18,2) NOT NULL CONSTRAINT CK_Invoices_InvoiceAmount CHECK (InvoiceAmount > 0),
    InvoiceDate    DATE NOT NULL,
    Status         NVARCHAR(20) NOT NULL CONSTRAINT DF_Invoices_Status DEFAULT 'Pending'
                   CONSTRAINT CK_Invoices_Status CHECK (Status IN ('Pending','Matched','Exception','Approved','Rejected')),
    MatchNote      NVARCHAR(50) NULL,
    CreatedBy      NVARCHAR(50) NOT NULL,
    CreatedAtUtc   DATETIME2(0) NOT NULL CONSTRAINT DF_Invoices_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    DecidedBy      NVARCHAR(50) NULL,
    DecidedAtUtc   DATETIME2(0) NULL,
    DecisionReason NVARCHAR(500) NULL,
    CONSTRAINT UQ_Invoices_Vendor_InvoiceNumber UNIQUE (VendorId, InvoiceNumber)
);
GO

-- Supports the queue query: filter on Status, sort by InvoiceDate
CREATE NONCLUSTERED INDEX IX_Invoices_Status_InvoiceDate
    ON dbo.Invoices (Status, InvoiceDate)
    INCLUDE (VendorId, InvoiceNumber, InvoiceAmount);
GO

CREATE TABLE dbo.AuditLog (
    AuditId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLog PRIMARY KEY,
    InvoiceId      INT NOT NULL CONSTRAINT FK_AuditLog_Invoices REFERENCES dbo.Invoices(InvoiceId),
    Action         NVARCHAR(30) NOT NULL,
    PerformedBy    NVARCHAR(50) NOT NULL,
    PerformedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_AuditLog_PerformedAtUtc DEFAULT SYSUTCDATETIME(),
    Details        NVARCHAR(600) NULL
);
GO
