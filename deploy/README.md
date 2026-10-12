# deploy/ — Helm charts (Phase 4)

GitOps source for Argo CD. Azure Pipelines bumps image digests here; Argo CD syncs `dev` automatically and `prod` after a manual approval. Argo Rollouts canaries (10% → 50% → 100%) with analysis on error rate and p95 latency.

Planned charts: `control-plane`, `agent-runtime`, `orbit-ui`, `otel-collector` values, sandbox Job template with NetworkPolicy.
