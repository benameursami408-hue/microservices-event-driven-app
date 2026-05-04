# Phase 6 - Microservices reliability

This phase improves the reliability and observability of the SAV microservices without changing the user-facing API contracts.

## What changed

### 1. Correlation IDs

The API Gateway and each backend service now use `X-Correlation-ID`.

Behavior:

- If the request already has `X-Correlation-ID`, the service preserves it.
- If it is missing, the service creates one.
- The response returns the same header.
- Logs include a `CorrelationId` scope.

Use this during debugging:

```bash
curl -H "X-Correlation-ID: demo-001" http://localhost:5005/api/reclamations
```

### 2. Readiness and liveness endpoints

Each service now exposes:

```txt
/health
/health/live
/health/ready
```

`/health/live` says the process is alive.

`/health/ready` checks:

- SQL Server connectivity through EF Core.
- RabbitMQ configuration presence.

Docker health checks now call `/health/ready`, so containers only become healthy when their required runtime dependencies are ready.

### 3. RabbitMQ retry policy

MassTransit now uses an exponential retry policy for consumers:

```txt
3 retries
minimum interval: 1 second
maximum interval: 30 seconds
interval delta: 2 seconds
```

MassTransit still handles its standard `_error` queues for failed consumer messages.

### 4. Outbox dispatcher logging and logical dead letters

The outbox dispatchers now log:

- correlation id
- event id
- event type
- event version
- producer
- success/failure

After the retry limit is reached, the message is no longer dispatched and `OutboxMessages.LastError` is prefixed with:

```txt
[DEADLETTER_AFTER_15_RETRIES]
```

This gives you a simple logical dead-letter strategy without adding risky database migrations.

### 5. Event contract documentation

A shared reference was added:

```txt
src/building-blocks/SharedEvents/references/EVENT_CONTRACT.md
```

It documents required event metadata and versioning rules.

## How to verify locally

Start the stack:

```bash
docker compose up --build
```

Check readiness:

```bash
curl http://localhost:5001/health/ready
curl http://localhost:5002/health/ready
curl http://localhost:5003/health/ready
curl http://localhost:5004/health/ready
curl http://localhost:5005/health/ready
```

Check correlation propagation:

```bash
curl -i -H "X-Correlation-ID: demo-phase6" http://localhost:5005/health/ready
```

You should see:

```txt
X-Correlation-ID: demo-phase6
```

## Remaining senior improvements for later

These are intentionally not done in Phase 6 because they require deeper schema/design changes:

1. Put business entity update + outbox insert in the same explicit database transaction.
2. Add real delayed retries with `NextAttemptAt` in the outbox table.
3. Add an outbox admin/debug endpoint protected by `ADMIN`.
4. Add idempotency to all ReclamationService projection consumers.
5. Add RabbitMQ connectivity health checks using a dedicated health-check package.
6. Add OpenTelemetry tracing and export to Jaeger/Tempo.

## Smoke-test scripts

Linux/macOS:

```bash
./tools/test-reliability.sh
```

Windows PowerShell:

```powershell
./tools/test-reliability.ps1
```
