# Dev Squad — 6 skills + 6 agents

| | Name | Skill | Stack |
|---|---|---|---|
| 🎼 | Maestro | maestro-orchestrator | Plans, routes, contracts, security gate |
| 🎨 | Pixel | pixel-angular-frontend | Angular 22, zoneless, signals, Vitest |
| 🔨 | Forge | forge-dotnet-backend | .NET 10 LTS, C# 14, minimal APIs |
| 🗺️ | Atlas | atlas-postgres-database | PostgreSQL 18, EF Core 10, pgvector |
| 🐍 | Viper | viper-python | Python 3.14, uv, FastAPI, RAG |
| ⛵ | Helm | helm-azure-devops | Azure DevOps YAML, ACR, Container Apps, gates |

Every specialist covers **code scanning** (CodeQL/Semgrep plus language linters), **secret scanning** (gitleaks plus push protection) and **dependency scanning** for its layer. Maestro runs the combined gate.

## Talk to them
- "Maestro, build policy renewal reminders end to end": a full plan, then handoffs.
- "@Atlas why is this query slow?": goes straight to one specialist.
- "Maestro, security audit this repo": every checklist plus the scan table.

## Install
**Claude app**: Settings → Capabilities → Skills → upload each zip in `skills/`.

**Claude Code**:
```bash
cp -r skills/* ~/.claude/skills/
cp agents/*.md ~/.claude/agents/
```
Then: `> use the maestro agent to add reminders to PolicyPing`

## Scanner tools to install locally
gitleaks · semgrep · trivy · checkov · squawk · uv (gives `uvx pip-audit`) · CodeQL via GHAzDO in pipelines.
