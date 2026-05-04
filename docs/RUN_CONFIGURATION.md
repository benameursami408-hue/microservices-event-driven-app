# Run Configuration

## Single source of truth

The public gateway URL is:

```txt
http://localhost:5005
```

The frontend must call only the gateway, not the individual services.

```txt
VITE_API_BASE_URL=http://localhost:5005
```

## Port matrix

| Component | Docker public port | Container/downstream port | Local dotnet port |
|---|---:|---:|---:|
| API Gateway | 5005 | 8080 | 5005 |
| AuthService | 5001 | 8080 | 5165 |
| ReclamationService | 5002 | 8080 | 5057 |
| NotificationService | 5003 | 8080 | 5242 |
| InterventionService | 5004 | 8080 | 5104 |
| SQL Server | 14333 | 1433 | 14333 |
| RabbitMQ | 5672 / 15672 | 5672 / 15672 | 5672 / 15672 |

## Docker mode

In Docker mode, the gateway loads `ocelot.json` and routes to service container names:

```txt
auth-service:8080
reclamation-service:8080
notification-service:8080
intervention-service:8080
```

## Local mode

In local dotnet mode, the gateway loads `ocelot.Development.json` and routes to local service ports:

```txt
AuthService: http://localhost:5165
ReclamationService: http://localhost:5057
NotificationService: http://localhost:5242
InterventionService: http://localhost:5104
```

## Environment variables

Docker Compose now fails clearly if required secret values are missing instead of silently using hardcoded passwords.

Create `.env` from `.env.example`:

```bash
cp .env.example .env
```

Then edit the placeholder values.

Required secret values:

```txt
SA_PASSWORD
RABBITMQ_PASS
JWT_SECRET
SEED_ADMIN_PASSWORD
```

## Health checks

After starting Docker Compose, verify:

```bash
curl http://localhost:5005/health
curl http://localhost:5001/health
curl http://localhost:5002/health
curl http://localhost:5003/health
curl http://localhost:5004/health
```
