# Phase 1 Stabilization Changes

## Completed

- Standardized public API Gateway port to `5005`.
- Updated Docker Compose gateway mapping from `5000:8080` to `${GATEWAY_HTTP_PORT:-5005}:8080`.
- Kept frontend API base URL on `http://localhost:5005`.
- Updated frontend README references from `5000` to `5005`.
- Added root `.gitignore` to prevent committing `.env`, `bin`, `obj`, `.vs`, `node_modules`, and `dist`.
- Removed the real `.env` file from the patched deliverable.
- Replaced hardcoded secret-like defaults in Docker Compose with required environment variable checks.
- Replaced secret-like values in backend `appsettings*.json` with obvious placeholders.
- Added root `README.md` and `docs/RUN_CONFIGURATION.md`.

## Why this matters

Before this phase, Docker exposed the gateway on `5000` while the frontend expected `5005`. That makes CRUD requests fail or hit the wrong port depending on how the project is launched.

After this phase, the frontend and gateway use the same public gateway URL:

```txt
http://localhost:5005
```

## Next phase

Phase 2 should fix Admin Users CRUD validation and 400 responses by aligning frontend form validation with backend DTO validation.
