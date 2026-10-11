# Orbit — Multi-Agent AI Engineering & Observability Platform

Oct 10, 2026 · @Bhuvanesh

## Vision

Orbit turns a ticket into deployed, monitored code, then turns production incidents back into tickets. It is one platform with two modules: **Squad Engine** (multi-agent software engineering) and **Observatory** (cloud deployment and observability).

The closed loop is the product:

1. A developer files an issue or a feature brief.
2. Squad Engine plans it and specialist agents write code, tests and migrations in a sandbox.
3. Agents open a pull request; the security gate and a human reviewer approve it.
4. Observatory deploys it to AKS through GitOps with a canary rollout.
5. Telemetry, SLOs and LLM traces watch the release.
6. When an alert fires, the incident agent writes a root-cause summary and opens a new issue for Squad Engine.

**Why build it:** it is a flagship AI Product Engineer project, and it closes the interview gaps in one build: event-driven messaging (Azure Service Bus), Kubernetes/AKS, Key Vault, LangGraph and a vector index. It also reuses the Maestro squad skills as the agents' playbooks.

## Architecture

Orbit runs as a handful of services on one AKS cluster, joined by Azure Service Bus and one Postgres database.

&#91;embedded content: Orbit architecture · 2 modules, closed loop\]

Read it top to bottom: issues flow into Squad Engine, merged PRs flow down into Observatory, and alerts loop back up the left side as new issues.

| Layer | Choice | Why |
| --- | --- | --- |
| UI | Angular 22 + SignalR | Live streaming of plans, agent messages and diffs |
| Control plane | .NET 10 minimal APIs | Your strongest stack; auth, webhooks, approvals, cost accounting |
| Agent runtime | Python 3.14 + LangGraph | Mature agent ecosystem; checkpoints and human-in-the-loop pauses |
| Messaging | Azure Service Bus topics | Durable events, dead-lettering, emulator for local dev |
| Data | PostgreSQL 18 + pgvector | One database for run state, checkpoints and the code index |
| Compute | AKS with system, apps and sandbox node pools | Isolation for untrusted agent code; KEDA autoscaling |
| Delivery | Azure DevOps, ACR, Argo CD, Argo Rollouts | Build once, GitOps deploy, canary with automatic rollback |
| Telemetry | OTel Collector, Prometheus, Loki, Tempo, Grafana | One open stack for metrics, logs, traces and LLM spans |
| Identity | Entra ID, Managed Identity, Key Vault, OIDC | No stored secrets in code or pipelines |

## Module A — Squad Engine

Squad Engine runs a LangGraph graph per issue: Maestro plans, specialists build in a sandbox, a reviewer and a security agent check, and a human approves the PR.

### Agents

| Agent | Role | Key tools |
| --- | --- | --- |
| Maestro | Plans steps, assigns owners, enforces contracts, decides when done | repo map, issue reader, plan store |
| Atlas | Schema, EF Core and Alembic migrations | file edit, `dotnet ef`, squawk |
| Forge | .NET 10 APIs and xUnit tests | file edit, `dotnet build/test` |
| Viper | Python services and LLM features | file edit, `uv run pytest` |
| Pixel | Angular 22 UI and Vitest tests | file edit, `ng build/test` |
| Helm | Pipelines, Dockerfiles, Helm charts | file edit, `helm lint`, checkov |
| Reviewer | Reads the full diff against the squad skills, requests changes | diff reader, comment writer |
| Sentinel | Runs the security gate and blocks on high findings | semgrep, gitleaks, trivy, pip-audit, npm audit |

Each agent's system prompt is its squad SKILL.md, so the rules you already wrote become the agents' behaviour.

### Run lifecycle

1. **Intake** — issue arrives by webhook (Azure DevOps or GitHub) or the Orbit UI.
2. **Index** — the repo is cloned and chunked into a code index (pgvector) so agents retrieve relevant files instead of reading the whole repo.
3. **Plan** — Maestro writes a step plan with owners; the user approves it in the UI (gate 1).
4. **Build** — each step runs as a sandboxed job; agents loop edit → build → test until green or a retry cap is hit.
5. **Review** — Reviewer and Sentinel run; failures go back to the owning agent.
6. **PR** — branch pushed, PR opened with plan, diff summary, test and scan results; a human merges (gate 2).

### Sandbox

- One Kubernetes Job per step on a dedicated AKS node pool, using an image with .NET 10, Node 24, Python 3.14 and the scanners.
- No secrets mounted, egress limited to package registries by NetworkPolicy, CPU, memory and time limits per job.
- The workspace is a fresh clone; the only way out is a commit to an agent branch.

### Services

- **Control plane (.NET 10):** runs, approvals, repos, users, webhooks, cost accounting; publishes events to Azure Service Bus.
- **Agent runtime (Python 3.14, LangGraph):** graph execution with checkpoints in Postgres, so a run resumes after a crash or an approval pause.
- **LLM gateway (Python):** one interface over model providers, per-run token budgets, retries, prompt and response logging with redaction.
- **Orbit UI (Angular 22):** live run view (plan, agent messages, diffs, test output) streamed over SignalR.

### Core data

`repos`, `issues`, `runs`, `run_steps`, `agent_messages`, `tool_calls`, `artifacts` (diffs, logs, reports), `approvals`, `llm_usage` (tokens, cost, latency per call), `code_chunks` (pgvector).

## Module B — Observatory

Observatory deploys every merged PR to AKS through GitOps, watches it with OpenTelemetry, and hands incidents back to Squad Engine.

### Deployment

- **Build:** the Azure DevOps pipeline builds images once, pushes them to ACR tagged by git SHA, and runs the security gate.
- **GitOps:** the pipeline bumps the image digest in a `deploy/` repo of Helm charts; **Argo CD** syncs it to the cluster. No `kubectl apply` from CI.
- **Progressive delivery:** **Argo Rollouts** canary (10% → 50% → 100%) with automatic analysis against error rate and p95 latency; it rolls back on a failed check.
- **Environments:** `dev` and `prod` namespaces (one cluster to save cost), with prod sync behind a manual approval.
- **Scaling:** **KEDA** scales agent workers on Service Bus queue length, down to zero when idle.

### Telemetry stack

- Every service exports OTLP to an **OpenTelemetry Collector** on the cluster.
- **Prometheus** (metrics), **Loki** (logs), **Tempo** (traces), **Grafana** (dashboards and alerts). Azure Monitor and Application Insights are an optional second exporter.
- One trace follows a request from the Angular UI through the .NET API, Service Bus and the Python runtime.
- **SLOs** per service: availability and p95 latency, with burn-rate alerts.

### LLM observability

- Every agent step and model call is a span using the OpenTelemetry GenAI semantic conventions: model, prompt and completion tokens, latency, cost, tool calls.
- Dashboards: cost per run, tokens per agent, retries per step, success rate per agent, time to green PR.
- **Evals:** a golden set of small issues on a sample repo runs nightly; the score is tracked over time, so a prompt or model change that drops quality shows up before it hurts.

### Incident agent

1. Grafana alert → webhook → Service Bus `incidents` topic.
2. The incident agent queries Prometheus, Loki and Tempo through read-only tools, and checks the latest deploys in Argo CD.
3. It writes a root-cause summary: what broke, when, which release, evidence links, suggested fix.
4. It opens an issue tagged `incident` and, if the cause is code, Squad Engine can pick it up as a normal run.
5. Rollback stays a human (or Argo Rollouts) decision; the agent never changes production on its own.

## Security and guardrails

Agents never touch `main`, production or secrets directly; every risky step has a human gate or a hard limit.

| Risk | Guardrail |
| --- | --- |
| Agent writes to the wrong place | Pushes only to `orbit/<run-id>` branches; branch policies require human approval to merge |
| Sandbox escape or data exfiltration | Ephemeral pod per step, no secrets mounted, egress allow-list, non-root, read-only root filesystem |
| Prompt injection from repo files, issues or logs | Repo, issue and log content is passed as data, never as instructions; tool calls are allow-listed per agent; the incident agent's tools are read-only |
| Leaked secrets in generated code | Sentinel runs gitleaks on every diff; push protection on the repo; a finding blocks the PR |
| Vulnerable code or dependencies | Semgrep or CodeQL, npm audit, NuGet audit, pip-audit, Trivy, Checkov and squawk in the gate; high or critical blocks |
| Runaway cost | Per-run token budget and step cap in the LLM gateway; the run pauses for approval when the budget is reached |
| Credentials for the platform itself | OIDC service connections, Managed Identity at runtime, Key Vault for provider keys |
| Sensitive data in traces | Prompt and response logging redacts tokens, keys and emails before export |

## 12-week plan

The plan ships a working issue-to-PR loop by week 4 and the full closed-loop demo by week 12, at roughly 10 hours a week.

&#91;embedded content: Orbit build plan · 6 phases, 12 weeks\]

Each phase ends at a Maestro checkpoint; do not start the next phase until its exit criterion is met.

1. **Foundations** — monorepo, docker-compose (Postgres 18 + pgvector, Service Bus emulator), pre-commit with gitleaks, CI with the security gate.
2. **Squad MVP** — control plane, LangGraph runtime with Maestro + Forge + Reviewer, local Docker sandbox, PR creation, basic run view.
3. **Full squad** — all specialist agents, Sentinel, code index, token budgets, golden eval set running nightly.
4. **Platform on AKS** — Bicep for AKS, ACR, Service Bus and Key Vault; Helm charts; Argo CD and Argo Rollouts; sandbox Jobs with NetworkPolicy; KEDA.
5. **Observability** — OTel in every service, Collector, Prometheus, Loki, Tempo, Grafana; SLOs; LLM cost and quality dashboards.
6. **Closed loop** — alert webhook, incident agent with read-only tools, issue creation, the five-minute demo recorded.

## Career value and kickoff

Orbit closes six skill gaps with working code, and the closed loop gives one demo that covers system design, AI and DevOps.

| Gap | Where Orbit covers it |
| --- | --- |
| Event-driven messaging | Service Bus topics for runs, steps and incidents; KEDA scaling on queue depth |
| Kubernetes / AKS | Sandbox Jobs, Helm charts, Argo CD, Argo Rollouts, NetworkPolicy |
| Azure PaaS | ACR, Key Vault, Service Bus, Managed Identity, OIDC connections |
| LangChain / LangGraph | The whole agent runtime, with checkpoints and human-in-the-loop pauses |
| Vector search | Code index in pgvector for repo retrieval |
| Observability | OTel end to end, SLOs, LLM tracing and evals |

### Interview talking points

- How you stopped agents from going off the rails: sandbox, allow-listed tools, budgets, two human gates.
- How a run survives a crash: LangGraph checkpoints in Postgres plus idempotent Service Bus consumers.
- How you measured agent quality: nightly eval score, success rate per agent, cost per green PR.
- How a bad deploy is caught: canary analysis on SLO metrics, then automatic rollback and an incident issue.

### Five-minute demo script

1. File an issue on a sample app: "add a due-date filter to the tasks list".
2. Approve Maestro's plan; watch agents work live in the Orbit UI.
3. Open the PR: plan, diff, tests and a green security gate.
4. Merge; show the Argo Rollouts canary and the Grafana dashboard.
5. Break it on purpose (inject latency); show the alert, the incident agent's root-cause summary and the new issue.

### First Maestro kickoff prompt

```text
Use the maestro agent. Build Orbit Phase 0 from docs/orbit-blueprint.md:
monorepo with control-plane (.NET 10), agent-runtime (Python 3.14 + LangGraph),
orbit-ui (Angular 22), deploy/ (Helm charts), infra/ (Bicep), docker-compose with
Postgres 18 + pgvector and the Service Bus emulator, pre-commit with gitleaks,
and a CI pipeline with the security gate. Stop for my approval after the plan.
```
