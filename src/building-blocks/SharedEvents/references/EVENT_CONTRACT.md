# Shared integration event contract

Every event in `SharedEvents.Events` must carry these metadata fields:

- `EventId`: unique idempotency key for consumers.
- `EventType`: stable business event name, for example `reclamation.created`.
- `EventVersion`: increment only for breaking payload changes.
- `CorrelationId`: request/workflow identifier propagated from HTTP `X-Correlation-ID`.
- `CausationId`: optional id of the command/event that caused the new event.
- `Producer`: service that produced the event.
- `OccurredAt`: UTC business occurrence time.

Reliability rules:

1. Consumers must treat `EventId` as the idempotency key.
2. Consumers must log `CorrelationId`, `EventId`, `EventType`, `EventVersion`, and `Producer`.
3. Breaking event payload changes require a new `EventVersion` and backward-compatible consumer handling.
4. Outbox dispatch failures are retried. Messages reaching the retry limit are logical dead letters and must be inspected in `OutboxMessages.LastError`.
