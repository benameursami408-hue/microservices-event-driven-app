# Phase 7 - Security cleanup

This phase hardens the SAV platform without changing the main business workflow.

## Changes applied

### Safer error responses

The global exception handlers no longer expose raw internal exception messages for unexpected `500` errors outside development. Every error response now includes:

- `traceId`
- `correlationId`

`UnauthorizedAccessException` now maps to `403 Forbidden` because the user is authenticated but does not have the right permission. Authentication middleware remains responsible for `401 Unauthorized` when the token is missing or invalid.

### Security headers

Every backend service and the API Gateway now adds basic hardening headers:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: no-referrer`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- `Cross-Origin-Opener-Policy: same-origin`

A stricter Content Security Policy is applied outside development to avoid breaking Swagger UI during local development.

### CORS hardening

The API Gateway keeps explicit allowed origins and no longer uses unrestricted headers/methods. Allowed headers are limited to:

- `Authorization`
- `Content-Type`
- `Accept`
- `Origin`
- `X-Correlation-ID`

Allowed methods are:

- `GET`, `POST`, `PUT`, `PATCH`, `DELETE`, `OPTIONS`

### Login/register rate limiting

AuthService now rate-limits sensitive auth endpoints:

- `POST /api/auth/login`
- `POST /api/auth/register`

Default local limit:

```txt
10 requests / minute / IP
```

Docker override:

```txt
AUTH_RATE_LIMIT_PER_MINUTE=10
```

### Upload hardening

Reclamation uploads now validate:

- upload kind: `image` or `proof`
- max size: 10 MB
- extension allow-list
- exact content type allow-list
- magic-number/file-signature match
- per-user upload rate limit

Default local upload limit:

```txt
20 uploads / minute / user or IP
```

Docker override:

```txt
UPLOAD_RATE_LIMIT_PER_MINUTE=20
```

### JWT validation

JWT validation now uses a smaller clock skew:

```txt
2 minutes
```

This avoids long acceptance windows for expired tokens while keeping small clock differences tolerable.

### Frontend correlation ID

The frontend now sends `X-Correlation-ID` with every API request, making browser issues easier to trace through the gateway and services.

## What remains for a real production version

For a professional production release, add:

1. Refresh tokens or secure HTTP-only cookie sessions.
2. HTTPS certificates and reverse proxy TLS termination.
3. Centralized secret management such as user-secrets, Docker secrets, Vault, or cloud secrets.
4. Antivirus scanning for uploaded files.
5. Audit log table for admin actions.
6. Account lockout policy after repeated failed logins.
7. Role/permission matrix documentation.

## Manual security smoke test

Run the platform, then execute:

```bash
./tools/test-security.sh
```

On Windows:

```powershell
./tools/test-security.ps1
```
