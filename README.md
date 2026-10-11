# Orbit — Multi-Agent AI Engineering & Observability Platform

Orbit turns a ticket into deployed, monitored code, then turns production incidents back into tickets.

- **Squad Engine** — LangGraph agents (Maestro, Atlas, Forge, Viper, Pixel, Helm, Reviewer, Sentinel) plan, build, review and open PRs from a sandbox.
- **Observatory** — GitOps deploys to AKS with canary rollouts, OpenTelemetry end to end, and an incident agent that files issues back to Squad Engine.

Full design: [docs/orbit-blueprint.md](docs/orbit-blueprint.md).

## Repo layout

| Path | What | Owner |
|---|---|---|
| `control-plane/` | .NET 10 minimal APIs: runs, approvals, webhooks, cost | 🔨 Forge |
| `agent-runtime/` | Python 3.14 + LangGraph agent runtime and LLM gateway | 🐍 Viper |
| `orbit-ui/` | Angular 22 live run view (SignalR) | 🎨 Pixel |
| `db/` | Postgres 18 + pgvector init scripts | 🗺️ Atlas |
| `deploy/` | Helm charts synced by Argo CD | ⛵ Helm |
| `infra/` | Bicep for AKS, ACR, Service Bus, Key Vault | ⛵ Helm |
| `.github/workflows/` | PR validation and security gate (GitHub Actions) | ⛵ Helm |
| `.azure-pipelines/` | Build once, deploy to AKS (Azure Pipelines) | ⛵ Helm |
| `.claude/` | Dev-squad skills and agents used to build Orbit | 🎼 Maestro |

`dev-squad/` is the source package of the squad skills; `archive/` holds superseded skill copies.

## Status

Phase 0 — Foundations (in progress). See the 12-week plan in the blueprint.

## Local tooling

.NET 10 SDK · Node 24 LTS · uv (Python 3.14) · Docker Desktop · gitleaks · semgrep · trivy · checkov · squawk · pre-commit · helm · az · kubectl
