# Messaging contract (⛵ Helm + 🔨 Forge + 🐍 Viper)

Azure Service Bus topics. Locally they come from the emulator config [local/servicebus/Config.json](../../local/servicebus/Config.json); in Azure from Bicep (Phase 4).

| Topic | Publisher | Subscription → consumer | Events (`subject`) |
|---|---|---|---|
| `runs` | control plane | `agent-runtime` → agent runtime | `run.requested`, `run.plan_approved`, `run.budget_approved`, `run.cancelled` |
| `steps` | agent runtime | `control-plane` → control plane (persists, pushes to SignalR) | `run.planned`, `step.started`, `step.message`, `step.finished`, `run.pr_opened`, `run.finished` |
| `incidents` | control plane (Grafana webhook) | `incident-agent` → agent runtime | `incident.opened` |

## Envelope

Body is JSON (`content_type: application/json`), CloudEvents-shaped:

```json
{
  "specversion": "1.0",
  "id": "0199f2c4-…",            // UUIDv7, also the Service Bus MessageId
  "type": "step.finished",       // same as the Service Bus Subject
  "source": "orbit/agent-runtime",
  "time": "2026-10-11T07:30:00Z",
  "runId": "0199f2c0-…",
  "data": { }
}
```

## Rules
- `MessageId` = event `id`. Duplicate detection is on (5 min in the emulator, longer in Azure), and consumers are idempotent anyway: they record processed ids before acknowledging.
- `CorrelationId` = `runId`; W3C `traceparent` goes in application properties so one trace spans UI → API → Service Bus → runtime.
- Consumers complete only after the side effect is committed; poison messages dead-letter after 5 deliveries.
- Message bodies never carry secrets or repo file contents. Large payloads (diffs, logs) go to `orbit.artifacts` and are referenced by id.
- Emulator limits: TTL ≤ 1 h, duplicate-detection window ≤ 5 min. Azure values are set in Bicep.
