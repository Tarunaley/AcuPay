/* AcuPay - 02_procedures.sql : all data access goes through these. Safe to re-run. */
USE AcuPay;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetInvoices
    @Status     NVARCHAR(20) = NULL,
    @VendorCode NVARCHAR(20) = NULL,
    @PageNumber INT,
    @PageSize   INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Result set 1: total count for the filter
    SELECT COUNT(*) AS TotalCount
    FROM dbo.Invoices i
    JOIN dbo.Vendors v ON v.VendorId = i.VendorId
    WHERE (@Status IS NULL OR i.Status = @Status)
      AND (@VendorCode IS NULL OR v.VendorCode = @VendorCode);

    -- Result set 2: the requested page
    SELECT i.InvoiceId, i.InvoiceNumber, i.VendorId, v.VendorCode, v.VendorName,
           i.PoId, po.PoNumber, po.VendorId AS PoVendorId, po.PoAmount, po.Status AS PoStatus,
           i.InvoiceAmount, i.InvoiceDate, i.Status, i.MatchNote, i.CreatedBy, i.CreatedAtUtc,
           i.DecidedBy, i.DecidedAtUtc, i.DecisionReason
    FROM dbo.Invoices i
    JOIN dbo.Vendors v ON v.VendorId = i.VendorId
    LEFT JOIN dbo.PurchaseOrders po ON po.PoId = i.PoId
    WHERE (@Status IS NULL OR i.Status = @Status)
      AND (@VendorCode IS NULL OR v.VendorCode = @VendorCode)
    ORDER BY i.InvoiceDate, i.InvoiceId
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetInvoiceById
    @InvoiceId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT i.InvoiceId, i.InvoiceNumber, i.VendorId, v.VendorCode, v.VendorName,
           i.PoId, po.PoNumber, po.VendorId AS PoVendorId, po.PoAmount, po.Status AS PoStatus,
           i.InvoiceAmount, i.InvoiceDate, i.Status, i.MatchNote, i.CreatedBy, i.CreatedAtUtc,
           i.DecidedBy, i.DecidedAtUtc, i.DecisionReason
    FROM dbo.Invoices i
    JOIN dbo.Vendors v ON v.VendorId = i.VendorId
    LEFT JOIN dbo.PurchaseOrders po ON po.PoId = i.PoId
    WHERE i.InvoiceId = @InvoiceId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_InsertInvoice
    @InvoiceNumber NVARCHAR(50),
    @VendorCode    NVARCHAR(20),
    @PoNumber      NVARCHAR(30) = NULL,
    @InvoiceAmount DECIMAL(18,2),
    @InvoiceDate   DATE,
    @CreatedBy     NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @VendorId INT, @IsActive BIT, @PoId INT = NULL, @NewId INT;

    SELECT @VendorId = VendorId, @IsActive = IsActive FROM dbo.Vendors WHERE VendorCode = @VendorCode;
    IF @VendorId IS NULL THROW 50001, 'VENDOR_NOT_FOUND', 1;
    IF @IsActive = 0     THROW 50002, 'VENDOR_INACTIVE', 1;

    IF @PoNumber IS NOT NULL
    BEGIN
        SELECT @PoId = PoId FROM dbo.PurchaseOrders WHERE PoNumber = @PoNumber;
        IF @PoId IS NULL THROW 50003, 'PO_NOT_FOUND', 1;
    END

    BEGIN TRANSACTION;
        INSERT dbo.Invoices (InvoiceNumber, VendorId, PoId, InvoiceAmount, InvoiceDate, Status, CreatedBy)
        VALUES (@InvoiceNumber, @VendorId, @PoId, @InvoiceAmount, @InvoiceDate, 'Pending', @CreatedBy);

        SET @NewId = SCOPE_IDENTITY();

        INSERT dbo.AuditLog (InvoiceId, Action, PerformedBy, Details)
        VALUES (@NewId, 'Created', @CreatedBy, CONCAT(@InvoiceNumber, ' / ', @VendorCode));
    COMMIT TRANSACTION;

    SELECT @NewId AS InvoiceId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_UpdateInvoiceMatch
    @InvoiceId   INT,
    @Status      NVARCHAR(20),
    @MatchNote   NVARCHAR(50),
    @DecidedBy   NVARCHAR(50) = NULL,   -- set only for system auto-approval
    @PerformedBy NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
        UPDATE dbo.Invoices
        SET Status = @Status,
            MatchNote = @MatchNote,
            DecidedBy = @DecidedBy,
            DecidedAtUtc = CASE WHEN @DecidedBy IS NULL THEN NULL ELSE SYSUTCDATETIME() END
        WHERE InvoiceId = @InvoiceId AND Status NOT IN ('Approved','Rejected');

        IF @@ROWCOUNT = 0 THROW 50010, 'INVALID_STATE', 1;

        INSERT dbo.AuditLog (InvoiceId, Action, PerformedBy, Details)
        VALUES (@InvoiceId,
                CASE WHEN @DecidedBy IS NULL THEN 'Match' ELSE 'AutoApprove' END,
                @PerformedBy,
                CONCAT(@Status, ' / ', @MatchNote));
    COMMIT TRANSACTION;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_DecideInvoice
    @InvoiceId INT,
    @Status    NVARCHAR(20),          -- Approved or Rejected
    @DecidedBy NVARCHAR(50),
    @Reason    NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
        UPDATE dbo.Invoices
        SET Status = @Status,
            DecidedBy = @DecidedBy,
            DecidedAtUtc = SYSUTCDATETIME(),
            DecisionReason = @Reason
        WHERE InvoiceId = @InvoiceId AND Status IN ('Matched','Exception');

        IF @@ROWCOUNT = 0 THROW 50010, 'INVALID_STATE', 1;

        INSERT dbo.AuditLog (InvoiceId, Action, PerformedBy, Details)
        VALUES (@InvoiceId,
                CASE WHEN @Status = 'Approved' THEN 'Approve' ELSE 'Reject' END,
                @DecidedBy,
                @Reason);
    COMMIT TRANSACTION;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetSpendByVendor
AS
BEGIN
    SET NOCOUNT ON;
    SELECT v.VendorCode, v.VendorName,
           SUM(i.InvoiceAmount) AS TotalSpend,
           COUNT(*) AS InvoiceCount
    FROM dbo.Invoices i
    JOIN dbo.Vendors v ON v.VendorId = i.VendorId
    WHERE i.Status = 'Approved'
    GROUP BY v.VendorCode, v.VendorName
    ORDER BY TotalSpend DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetUserByUsername
    @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, Username, PasswordHash, Role FROM dbo.Users WHERE Username = @Username;
END
GO
