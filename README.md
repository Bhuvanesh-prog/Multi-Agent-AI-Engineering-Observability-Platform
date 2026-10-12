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

Phase 0 — Foundations (in review). See the 12-week plan in the blueprint.

## Run locally

```bash
cp .env.example .env            # fill the passwords; set ACCEPT_EULA=Y to accept the SQL Server / emulator EULAs
docker compose up -d --wait     # Postgres 18 + pgvector on 127.0.0.1:5433, Service Bus emulator on 5672 (health :5300)
pre-commit install              # gitleaks + ruff + hygiene hooks

# control plane → http://localhost:5080 (Scalar UI at /scalar in Development)
dotnet user-secrets set "ConnectionStrings:Orbit" "<value of ConnectionStrings__Orbit from .env>" --project control-plane/src/Orbit.ControlPlane.Api
dotnet run --project control-plane/src/Orbit.ControlPlane.Api

# agent runtime → http://127.0.0.1:8081 (reads ORBIT_* from ../.env)
cd agent-runtime && uv run orbit-agent-runtime

# UI → http://localhost:4200
cd orbit-ui && npm ci && npm start
```

Tests: `dotnet test` (control-plane), `uv run pytest` (agent-runtime), `npx ng test --watch=false` (orbit-ui). All need Docker for Testcontainers except the UI.

Windows notes: the `&` in this folder's name breaks npm's `.cmd` shims, so run the Angular CLI as `node node_modules/@angular/cli/bin/ng.js …`. Use `127.0.0.1`, not `localhost`, in database URLs (async psycopg does not fall back from IPv6).

## Local tooling

.NET 10 SDK · Node 24 LTS · uv (Python 3.14) · Docker Desktop · gitleaks · semgrep · trivy · checkov · squawk · pre-commit · helm · az · kubectl
