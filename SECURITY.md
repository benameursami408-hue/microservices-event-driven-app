# Security notes for SAV project

## Secrets

Never commit `.env` or real credentials. Use `.env.example` only as a template.

Rotate these values if they were shared publicly:

- `SA_PASSWORD`
- `RABBITMQ_PASS`
- `JWT_SECRET`
- `SEED_ADMIN_PASSWORD`

## Local demo defaults

The project is configured for a local PFE/demo environment. Public ports are bound to `127.0.0.1` in Docker Compose where possible.

## Authentication

JWT is used for API authentication. Frontend token storage currently uses `localStorage`, which is acceptable for a student demo but should be replaced by a safer session strategy for production.

Recommended production improvement:

- short-lived access tokens
- refresh tokens stored in secure HTTP-only cookies
- logout/refresh endpoint
- token revocation or rotation

## Uploads

Uploads are restricted by extension, MIME type, size, and file signature. For production, add antivirus scanning and external object storage.

## Reporting security issues

For this PFE project, document security issues in the project report and fix them before public deployment.
