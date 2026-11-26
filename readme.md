# AcuPay - Vendor Invoice Approval System

Web API that takes vendor invoices, matches each one to its purchase order, auto-approves the safe ones and routes the rest to a human. A small jQuery screen sits on top.

**Stack:** ASP.NET Core Web API (.NET 10), ADO.NET + stored procedures, SQL Server, jQuery, GitHub Actions.

## Team and ownership

| Slice | Owner |
|---|---|
| Data (schema, procedures, seed, repositories) | _name 1_ |
| API (domain rules, controllers, DTOs, validation, errors, logging) | _name 2_ |
| Front end and delivery (jQuery, CI, README, runbook) | _name 3_ |

## Proof that CI fails on a failing test

PR link: _paste the PR URL here_ (screenshot of the red check is attached to that PR).

## Run it on a clean machine

Prerequisites: .NET 10 SDK, SQL Server (local or Docker), `sqlcmd` or SSMS, Python or Node (only to serve `web/`).

1. Clone the repo and open a terminal in the root folder.
2. Create the database objects. In SSMS or `sqlcmd`, run in order: `db/01_schema.sql`, `db/02_procedures.sql`, `db/03_seed.sql`.
3. (Recommended) Run `db/04_app_login.sql` after replacing the password placeholder. It creates a login that can only execute the procedures.
4. Create the solution file and add the projects (only once, skip if one is committed):
   ```
   dotnet new sln -n AcuPay
   dotnet sln add src/AcuPay.Domain src/AcuPay.Data src/AcuPay.Api tests/AcuPay.Tests
   ```
5. Store the secrets locally (they are never committed):
   ```
   dotnet user-secrets init --project src/AcuPay.Api
   dotnet user-secrets set "ConnectionStrings:AcuPayDb" "Server=localhost;Database=AcuPay;User Id=acupay_app;Password=<your password>;TrustServerCertificate=True;Connect Timeout=5" --project src/AcuPay.Api
   dotnet user-secrets set "Jwt:Key" "<a random string of 32+ characters>" --project src/AcuPay.Api
   ```
6. Run the tests: `dotnet test`.
7. Start the API: `dotnet run --project src/AcuPay.Api`. Swagger opens at http://localhost:5080/swagger (Development only).
8. Serve the front end from the `web/` folder on port 5500: `cd web && python -m http.server 5500`, then open http://localhost:5500.
9. Sign in with `requester1 / Req@12345`, `approver1 / App@12345` or `finance1 / Fin@12345`.

If the API runs on another port, change `API_BASE` at the top of `web/app.js` and the allowed origins in `appsettings.json` (`Cors:AllowedOrigins`).

In CI or on a server use environment variables instead of user-secrets: `ConnectionStrings__AcuPayDb` and `Jwt__Key`.

## Business rules at a glance

- Matching order: NO_PO, VENDOR_MISMATCH, PO_CLOSED, OVER_PO_AMOUNT (> +2.00%), WITHIN_TOLERANCE, UNDER_PO_AMOUNT. Variance is rounded to 2 decimals first.
- A Matched invoice at or below 50,000 is auto-approved by SYSTEM (`AUTO_APPROVED`). Exceptions are never auto-approved.
- Approve/reject: Approver and Finance only, never your own invoice, only Matched/Exception invoices, exception override needs Finance plus a reason, a reject needs a reason of 10+ characters.
- All constants live in `appsettings.json` under `AcuPay`.

## Running the section 10.5 test script

The script assumes the state in section 10.4, so run `POST /api/invoices/{id}/match` on every seeded invoice (1 to 8) once before steps 6 onward. Steps 4 and 5 are then simply the first two of those calls.

| # | Action | Expected | Actual |
|---|---|---|---|
| 1 | Login requester1, wrong password | 401 INVALID_CREDENTIALS | |
| 2 | Login requester1 / Req@12345 | 200, token with role Requester | |
| 3 | GET /api/invoices, no token | 401 | |
| 4 | Match invoice 1 as requester1 | 200, Matched, variance 1.50 | |
| 5 | Match invoice 2 as requester1 | 200, Approved, decidedBy SYSTEM | |
| 6 | Approve invoice 1 as requester1 | 403 FORBIDDEN_ROLE | |
| 7 | Approve invoice 3 as approver1 | 403 SELF_APPROVAL_BLOCKED | |
| 8 | Approve invoice 4 as approver1 with reason | 403 OVERRIDE_REQUIRES_FINANCE | |
| 9 | Approve invoice 4 as finance1, no reason | 400 REASON_REQUIRED | |
| 10 | Approve invoice 4 as finance1, 20-char reason | 200, Approved, reason stored, audit row | |
| 11 | Reject invoice 2 as approver1 with reason | 409 INVALID_STATE | |
| 12 | Reject invoice 1 as approver1, short reason | 400 REASON_REQUIRED | |
| 13 | Create duplicate V002 / INV-5008 | 409 DUPLICATE_INVOICE | |
| 14 | GET /api/invoices?status=Exception&page=1 | 200, totalCount 3 | |
| 15 | GET spend-by-vendor as approver1 | 403 FORBIDDEN_ROLE | |
| 16 | GET /api/health, database stopped | 503, database unreachable | |

## Git workflow

Branches `feature/<owner>-<description>`, imperative commit messages, every change through a reviewed pull request, nobody merges their own PR, `main` stays green.
