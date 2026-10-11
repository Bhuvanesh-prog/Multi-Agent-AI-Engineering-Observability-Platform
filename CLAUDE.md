# Orbit — working notes for Claude

- The design is [docs/orbit-blueprint.md](docs/orbit-blueprint.md). Build phases are numbered 0–5 (Foundations = 0); stop for user approval at each phase's Maestro checkpoint.
- Work through the dev-squad in `.claude/`: Maestro plans and routes, specialists own their layers (Atlas DB, Forge .NET, Viper Python, Pixel Angular, Helm delivery). Each specialist's `SKILL.md` conventions and scan rules are mandatory.
- Repo host is **GitHub**. PR validation and the security gate run in **GitHub Actions** (`.github/workflows/`); build-and-deploy to AKS runs in **Azure Pipelines** (`.azure-pipelines/`).
- Schema ownership: EF Core (control plane) owns the `orbit` schema; the LangGraph checkpointer owns the `agent` schema.
- Telemetry goes OTLP → OpenTelemetry Collector (Prometheus, Loki, Tempo, Grafana); Application Insights is an optional second exporter.
- Never commit secrets: `.env` is git-ignored, `.env.example` holds placeholders, gitleaks runs in pre-commit and CI.
- Agents never push to `main`; Orbit's own agent branches are `orbit/<run-id>`.
