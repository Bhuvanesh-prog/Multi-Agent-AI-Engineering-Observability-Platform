---
name: azure-devops-cicd
description: Use when creating or reviewing Azure DevOps YAML pipelines, CI/CD for .NET 8 APIs and Angular apps, Docker images, Azure deployments (App Service, Container Apps, Static Web Apps), environments and approvals, secrets, and branching/release strategy.
---

# Azure DevOps CI/CD

Apply these conventions whenever writing or reviewing pipelines and deployment config. If the existing setup clearly follows a different convention, follow it and mention the difference once.

## Repo layout
```
.azure-pipelines/
  ci.yml                    # PR validation
  cd.yml                    # build once, deploy through environments
  templates/
    build-dotnet.yml
    build-angular.yml
    deploy-webapp.yml
    db-migrate.yml
```
- YAML pipelines only — no classic release pipelines in new work.
- Reuse through `templates/` with typed `parameters:`; keep the top-level pipeline short and readable.

## Branching
- Trunk-based: short-lived feature branches → PR into `main`.
- Branch policies on `main`: required reviewers (min 1), linked work item, CI build must pass, comments resolved, no direct pushes.
- Release tags `vX.Y.Z` (or GitVersion) for traceability.

## CI (every PR)
- Triggers: `pr: [main]`, `trigger: none` for the CI file; path filters so frontend-only changes skip backend jobs.
- .NET job: `UseDotNet@2` (8.x) → `dotnet restore` (with NuGet cache via `Cache@2`) → `dotnet build -c Release --no-restore` → `dotnet test --no-build --collect "XPlat Code Coverage"` → `PublishCodeCoverageResults@2`.
- Angular job: `NodeTool@0` (LTS) → `npm ci` (cache `~/.npm`) → `npm run lint` → `npm run test -- --watch=false --code-coverage` → `npm run build -- --configuration production`.
- Fail fast: warnings as errors, lint failures block merge.
- Security: `dotnet list package --vulnerable`, `npm audit --audit-level=high`, and secret scanning (GitHub Advanced Security for Azure DevOps if available).

## CD (build once, promote everywhere)
- Stages: `Build` → `Dev` → `QA` → `Prod`. The same artifact is promoted; never rebuild per environment.
- Use `deployment:` jobs bound to Azure DevOps `environment:`s; put approvals and checks (manual approval, business hours, branch control = `main` only) on the environment, not in YAML.
- Publish artifacts with `PublishPipelineArtifact@1`; download with `download: current`.
- Environment-specific config comes from variable groups / App Service settings, never baked into the build. For Angular, either one build per env config or a runtime `config.json` loaded at startup (preferred: build once).

## Database deployment
- Generate an EF Core migrations bundle in Build (`dotnet ef migrations bundle --self-contained -r linux-x64`) and publish it as an artifact.
- Run the bundle in each environment **before** the app deploy, using a migration-owner connection string from Key Vault.
- Prod: require approval before the migrate step; back up/snapshot first for destructive changes.

## Deployment targets
- API: Azure App Service (`AzureWebApp@1`) with **deployment slots** — deploy to `staging`, warm up/smoke test `/health`, then swap. Or Azure Container Apps for containerised workloads.
- Angular: Azure Static Web Apps (`AzureStaticWebApp@0`) or Storage + CDN/Front Door. Set SPA fallback routing.
- Containers: multi-stage Dockerfile (sdk → aspnet runtime, non-root user), `Docker@2` buildAndPush to ACR, tag with `$(Build.BuildId)` and the git SHA — never deploy `latest`.

## Secrets and connections
- Service connections use **workload identity federation** (OIDC), not client secrets.
- Secrets in Azure Key Vault, linked to a variable group (`AzureKeyVault@2` or linked group). Mark any inline secret variables as secret; never `echo` them.
- App runtime uses **Managed Identity** to reach Key Vault, ACR, and the database where supported.
- Scope service connections per environment (prod connection only usable by prod stage).

## Infrastructure as code
- Bicep (or Terraform) in `infra/`, deployed by its own pipeline with `what-if` / `plan` on PR and apply on approval.
- No manual portal changes in QA/Prod; if one happens, codify it immediately.

## Observability and rollback
- Application Insights connection string as an app setting; release annotations on deploy.
- Post-deploy smoke test step hitting `/health` and a key endpoint; fail the stage on non-200.
- Rollback = swap slots back (App Service) or redeploy the previous artifact/revision. Write migrations so the previous app version still works (expand/contract).

## Review checklist
- [ ] PR validation runs build, tests, lint, coverage, vulnerability checks
- [ ] Build once, promote same artifact through environments
- [ ] Approvals/checks on environments, prod limited to `main`
- [ ] Secrets in Key Vault, OIDC service connections, Managed Identity at runtime
- [ ] Migrations run as a separate, approved step before app deploy
- [ ] Slot swap / revision deploy with smoke test and a clear rollback path
- [ ] Images tagged immutably, no `latest`

## Output
When generating pipelines, give the full YAML files with paths, list the variable groups, service connections and environments that must exist in Azure DevOps, and note any one-time setup.
