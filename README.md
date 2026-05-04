# SAV Microservices Platform

This repository contains the SAV platform backend and frontend.

## Runtime standard

The project now uses one public API Gateway URL in local development and Docker mode:

```txt
http://localhost:5005
```

Keep this value aligned in:

- `docker-compose.yml` -> `api-gateway` published port
- root `.env` -> `GATEWAY_HTTP_PORT=5005`
- `front/.env.local` or `front/.env` -> `VITE_API_BASE_URL=http://localhost:5005`
- `src/gateway/ApiGateway/Properties/launchSettings.json`
- `src/gateway/ApiGateway/ocelot*.json` -> `GlobalConfiguration.BaseUrl`

## First setup

Copy the environment template and edit the placeholder secrets:

```bash
cp .env.example .env
```

Required values to change in `.env`:

```txt
SA_PASSWORD
RABBITMQ_PASS
JWT_SECRET
SEED_ADMIN_PASSWORD
```

Then configure the frontend:

```bash
cp front/.env.example front/.env.local
```

## Run with Docker Compose

From the repository root:

```bash
docker compose up --build
```

Expected local URLs:

| Component | URL |
|---|---|
| Frontend | http://localhost:5173 |
| API Gateway | http://localhost:5005 |
| AuthService | http://localhost:5001 |
| ReclamationService | http://localhost:5002 |
| NotificationService | http://localhost:5003 |
| InterventionService | http://localhost:5004 |
| RabbitMQ UI | http://localhost:15672 |
| SQL Server | localhost,14333 |

## Run the frontend

```bash
cd front
npm install
npm run dev
```

## Local backend mode

For mixed local development, start only SQL Server and RabbitMQ first:

```bash
docker compose up sqlserver rabbitmq
```

Then run the services with their existing launch settings:

| Service | Local port |
|---|---:|
| AuthService | 5165 |
| ReclamationService | 5057 |
| NotificationService | 5242 |
| InterventionService | 5104 |
| ApiGateway | 5005 |

The gateway uses `ocelot.Development.json` in this mode.

## Important repository rule

Never commit `.env`, real passwords, generated build folders, or IDE state folders.

Ignored examples:

```txt
.env
**/bin/
**/obj/
**/.vs/
front/node_modules/
front/dist/
```

## Phase 2 admin CRUD notes

Admin user create/update validation was stabilized in `docs/PHASE2_ADMIN_CRUD.md`. Start testing from `/app/admin/users` after logging in as an `ADMIN`.


## Phase 3 - Guide de test et UX demo

La phase 3 ajoute une page `/app/guide-test`, un parcours conseille par role et une documentation dediee: `docs/PHASE3_UX_DEMO_GUIDE.md`.

## Phase 4 - Refactorisation

La Phase 4 a decoupe les gros services backend en classes partielles et a extrait les sections de `ReclamationDetailPage.jsx` vers des composants dedies. Voir `docs/PHASE4_REFACTOR.md`.

## Phase 5 - Tests

Phase 5 adds a repeatable QA layer for the project:

- backend xUnit tests for Auth, Reclamation, Intervention, and Notification workflows;
- frontend Vitest unit tests for validation and demo helpers;
- Playwright E2E smoke tests for the guided demo flow;
- documentation in `docs/PHASE5_TEST_STRATEGY.md`;
- helper scripts in `tools/`.

Run frontend checks:

```bash
cd front
npm install
npm run lint
npm run test
npm run build
```

Run backend checks:

```bash
./tools/test-backend.sh
```



## Phase 6 reliability notes

See `docs/PHASE6_RELIABILITY.md` for correlation IDs, readiness endpoints, retry behavior, and outbox logical dead-letter handling.

## Phase 7 - Security cleanup

Security hardening was added for safer exception responses, security headers, stricter CORS, auth/upload rate limiting, upload signature validation, JWT clock skew, and security smoke tests. See `docs/PHASE7_SECURITY.md` and `SECURITY.md`.

## Backend run hotfix note

This package includes `docs/BACKEND_RUN_FIX.md`. For the fastest backend run:

```bash
docker compose down -v
docker compose up --build
```

The compose file has local defaults, so copying `.env.example` is recommended but no longer required just to start the backend.
