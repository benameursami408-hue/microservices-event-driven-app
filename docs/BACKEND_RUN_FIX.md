# Backend run fix

This package is a run-friendly hotfix of the Phase 7 project.

## What was changed

- `docker-compose.yml` now has safe local default values, so `docker compose up --build` does not fail when `.env` is missing.
- `appsettings.Development.json` files now contain local development values matching `.env.example`, so running services locally with `dotnet run` is easier.
- These values are for local development only. Replace them before deploying outside localhost.

## Recommended way to run backend

From the project root:

```bash
docker compose down -v
docker compose up --build
```

Then check:

```bash
curl http://localhost:5005/health/ready
curl http://localhost:5001/health/ready
curl http://localhost:5002/health/ready
curl http://localhost:5003/health/ready
curl http://localhost:5004/health/ready
```

## Admin login seed

When Docker starts successfully, the default development admin is:

```txt
email: admin@local
password: ChangeMe_Admin_2026!
```

## If you run locally without Docker

Start only SQL Server and RabbitMQ:

```bash
docker compose up -d sqlserver rabbitmq
```

Then run services separately:

```bash
cd src/services/AuthService/AuthService.Api && dotnet run
cd src/services/ReclamationService/ReclamationService.Api && dotnet run
cd src/services/NotificationService/NotificationService.Api && dotnet run
cd src/services/InterventionService/InterventionService.Api && dotnet run
cd src/gateway/ApiGateway && dotnet run
```

You need .NET SDK 9 because the project targets `net9.0`.

## If it still fails

Run:

```bash
docker compose logs auth-service --tail=120
docker compose logs reclamation-service --tail=120
docker compose logs intervention-service --tail=120
docker compose logs notification-service --tail=120
docker compose logs api-gateway --tail=120
```

Copy the first real error message.
