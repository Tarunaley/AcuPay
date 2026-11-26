# AcuPay Runbook

## 1. Is it healthy?

`GET /api/health` (no token needed).

- 200 with `"status": "Healthy"` and `"database": "Reachable"`: all good. The `environment` field tells you which configuration is loaded.
- 503 with `"database": "Unreachable"`: the API is up but cannot reach SQL Server (see failure 1).

Every response carries an `X-Correlation-Id` header and every error body carries `correlationId`. Search the API logs for that id to see everything that happened in that request.

## 2. The three most likely failures

### 1. Health returns 503 / every call fails with 500
Look first at: the log line `Database health check failed` (it has the SQL error). Then check the SQL Server service is running, the connection string secret (`ConnectionStrings__AcuPayDb`) is correct, the `acupay_app` login exists and has EXECUTE on the procedures, and nothing is blocking the port.

### 2. Everyone gets 401 after login
Look first at: whether `Jwt__Key`, `Jwt:Issuer` and `Jwt:Audience` are the same as when the token was issued (a changed key invalidates all tokens), then the server clock (tokens expire after `TokenExpiryMinutes`, 60). Decode the token at jwt.io to check `exp`, `role` and `name`.

### 3. A specific action returns 403, 409 or 400 unexpectedly
Look first at: the `error.code` in the response and the matching log line (`Business rule failed: <code>`). `SELF_APPROVAL_BLOCKED` means the caller created the invoice, `INVALID_STATE` means the invoice is Pending/Approved/Rejected (run match first), `OVERRIDE_REQUIRES_FINANCE` means an Exception invoice needs the Finance role. Check the `AuditLog` table for the invoice's history.

## 3. Roll back to the previous tag

```
git fetch --tags
git tag --list                      # find the previous tag, e.g. v1.0.0
git checkout v1.0.0
dotnet publish src/AcuPay.Api -c Release -o ./publish
```
Redeploy the `publish` folder and restart the API. Database changes are additive scripts; if a release changed the schema, restore the matching database backup before redeploying the older build.

## 4. Incident note template (day 6)

- What broke:
- How we found it (logs, correlation id, health endpoint):
- What we changed (hotfix branch and PR link):
- How we verified the fix:
