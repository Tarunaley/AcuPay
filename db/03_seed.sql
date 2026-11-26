/* AcuPay - 03_seed.sql : seed data from the brief. Passwords are stored as salted PBKDF2-SHA256 hashes
   (format: iterations.salt.hash, base64). No plain text here. Run on a freshly created schema. */
USE AcuPay;
GO

INSERT dbo.Users (Username, PasswordHash, Role) VALUES
 (N'requester1', N'100000.GiT9DG+YN46u4AhtMfnxjw==.tFcdEXXkw1Ec3eO73h994dc5UVygJs517K93T+LwVDQ=', 'Requester'),
 (N'approver1',  N'100000.w/mkSiOixy3hk/ZT6HqIKA==.cVATx025jbhGZrvUFPWOm4VTUAUtjkPqHJf8xEQ88aw=', 'Approver'),
 (N'finance1',   N'100000.C88d8acoYg0m2nkzTHkgjQ==.dyH+5tw1pHlxnLfbGvWo2jBqwy1LvLJ41YypwrlbSlA=', 'Finance');
GO

INSERT dbo.Vendors (VendorCode, VendorName, IsActive) VALUES
 ('V001', 'Alpha Infotech', 1),
 ('V002', 'Beta Supplies',  1),
 ('V003', 'Gamma Traders',  0);
GO

INSERT dbo.PurchaseOrders (PoNumber, VendorId, PoAmount, PoDate, Status) VALUES
 ('PO-1001', (SELECT VendorId FROM dbo.Vendors WHERE VendorCode='V001'), 100000.00, '2026-10-01', 'Open'),
 ('PO-1002', (SELECT VendorId FROM dbo.Vendors WHERE VendorCode='V001'),  50000.00, '2026-10-03', 'Open'),
 ('PO-1003', (SELECT VendorId FROM dbo.Vendors WHERE VendorCode='V002'),  20000.00, '2026-10-05', 'Open'),
 ('PO-1004', (SELECT VendorId FROM dbo.Vendors WHERE VendorCode='V002'),  75000.00, '2026-10-07', 'Closed'),
 ('PO-1005', (SELECT VendorId FROM dbo.Vendors WHERE VendorCode='V003'),  40000.00, '2026-10-09', 'Open');
GO

-- Inserted in order so InvoiceId 1..8 = INV-5001..INV-5008
INSERT dbo.Invoices (InvoiceNumber, VendorId, PoId, InvoiceAmount, InvoiceDate, Status, CreatedBy) VALUES
 ('INV-5001', (SELECT VendorId FROM dbo.Vendors WHERE VendorCode='V001'), (SELECT PoId FROM dbo.PurchaseOrders WHERE PoNumber='PO-1001'), 101500.00, '2026-10-10', 'Pending', 'requester1'),
 ('INV-5002', (SELECT VendorId FROM dbo.Vendors WHERE VendorCode='V001'), (SELECT PoId FROM dbo.PurchaseOrders WHERE PoNumber='PO-1002'),  49000.00, '2026-10-11', 'Pending', 'requester1'),
 ('INV-5003', (SELECT VendorId FROM dbo.Vendors WHERE VendorCode='V002'), (SELECT PoId FROM dbo.PurchaseOrders WHERE PoNumber='PO-1003'),  23000.00, '2026-10-12', 'Pending', 'approver1'),
 ('INV-5004', (SELECT VendorId FROM dbo.Vendors WHERE VendorCode='V002'), NULL,                                                           12000.00, '2026-10-13', 'Pending', 'requester1'),
 ('INV-5005', (SELECT VendorId FROM dbo.Vendors WHERE VendorCode='V002'), (SELECT PoId FROM dbo.PurchaseOrders WHERE PoNumber='PO-1004'),  70000.00, '2026-10-14', 'Pending', 'requester1'),
 ('INV-5006', (SELECT VendorId FROM dbo.Vendors WHERE VendorCode='V001'), (SELECT PoId FROM dbo.PurchaseOrders WHERE PoNumber='PO-1003'),  19000.00, '2026-10-15', 'Pending', 'requester1'),
 ('INV-5007', (SELECT VendorId FROM dbo.Vendors WHERE VendorCode='V003'), (SELECT PoId FROM dbo.PurchaseOrders WHERE PoNumber='PO-1005'),  36000.00, '2026-10-16', 'Pending', 'requester1'),
 ('INV-5008', (SELECT VendorId FROM dbo.Vendors WHERE VendorCode='V002'), (SELECT PoId FROM dbo.PurchaseOrders WHERE PoNumber='PO-1003'),  20100.00, '2026-10-17', 'Pending', 'requester1');
GO

INSERT dbo.AuditLog (InvoiceId, Action, PerformedBy, Details)
SELECT InvoiceId, 'Created', CreatedBy, 'Seeded' FROM dbo.Invoices;
GO
