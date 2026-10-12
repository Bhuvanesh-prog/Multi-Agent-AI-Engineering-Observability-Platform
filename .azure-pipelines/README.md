# .azure-pipelines/ — build once and deploy (Phase 4)

PR validation and the security gate run in GitHub Actions (`.github/workflows/`). Azure Pipelines, connected to this GitHub repo, owns delivery: build images once, push to ACR tagged by git SHA, generate SBOMs, then update the image digest in `deploy/` for Argo CD. Service connections use workload identity federation (OIDC).
