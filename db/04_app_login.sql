/* AcuPay - 04_app_login.sql : least-privilege SQL login for the API (execute on procedures only, no db_owner).
   Run once as an admin. Replace the password placeholder; do NOT commit a real password. */
USE master;
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'acupay_app')
    CREATE LOGIN acupay_app WITH PASSWORD = N'<REPLACE_WITH_STRONG_PASSWORD>', CHECK_POLICY = ON;
GO
USE AcuPay;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'acupay_app')
    CREATE USER acupay_app FOR LOGIN acupay_app;
GO
GRANT EXECUTE ON OBJECT::dbo.usp_GetInvoices        TO acupay_app;
GRANT EXECUTE ON OBJECT::dbo.usp_GetInvoiceById     TO acupay_app;
GRANT EXECUTE ON OBJECT::dbo.usp_InsertInvoice      TO acupay_app;
GRANT EXECUTE ON OBJECT::dbo.usp_UpdateInvoiceMatch TO acupay_app;
GRANT EXECUTE ON OBJECT::dbo.usp_DecideInvoice      TO acupay_app;
GRANT EXECUTE ON OBJECT::dbo.usp_GetSpendByVendor   TO acupay_app;
GRANT EXECUTE ON OBJECT::dbo.usp_GetUserByUsername  TO acupay_app;
GO
