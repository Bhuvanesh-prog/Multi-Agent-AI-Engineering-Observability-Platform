# Schema contract — draft (🗺️ Atlas)

Status: **draft for Phase 1**. No migrations exist yet; EF Core (control plane) will create these in the `orbit` schema, run as `orbit_owner`. The LangGraph checkpointer owns the `agent` schema.

Conventions: snake_case, plural tables, UUIDv7 primary keys (`Guid.CreateVersion7()` in .NET, `uuidv7()` in SQL), `timestamptz` UTC, enums as `text` + check constraint, every FK indexed, `xmin` mapped as row version, `created_at` / `updated_at` set by a `SaveChangesInterceptor`.

## Roles (local: `db/init/01-orbit.sh`; Azure: Entra ID roles)

| Role | Rights | Used by |
|---|---|---|
| `orbit_owner` | Owns `orbit` and `agent` schemas; DDL | EF migrations bundle, LangGraph checkpointer setup |
| `orbit_app` | DML only on `orbit` and `agent`; `statement_timeout=30s` | control plane, agent runtime at runtime |

## Tables

```sql
create table orbit.repos (
  id            uuid primary key default uuidv7(),
  provider      text not null check (provider in ('github')),
  owner         text not null,
  name          text not null,
  default_branch text not null default 'main',
  created_at    timestamptz not null default now(),
  updated_at    timestamptz not null default now(),
  constraint ux_repos_provider_owner_name unique (provider, owner, name)
);

create table orbit.issues (
  id          uuid primary key default uuidv7(),
  repo_id     uuid not null references orbit.repos(id) on delete cascade,
  external_id text not null,                       -- GitHub issue number
  title       text not null,
  body        text not null default '',
  source      text not null check (source in ('webhook','ui','incident')),
  created_at  timestamptz not null default now(),
  constraint ux_issues_repo_id_external_id unique (repo_id, external_id)
);

create table orbit.runs (
  id            uuid primary key default uuidv7(),
  issue_id      uuid not null references orbit.issues(id) on delete restrict,
  status        text not null check (status in
                  ('queued','indexing','planning','awaiting_plan_approval','building',
                   'reviewing','awaiting_merge','succeeded','failed','cancelled','budget_paused')),
  branch        text,                              -- orbit/<run-id>
  pr_url        text,
  token_budget  integer not null check (token_budget > 0),
  tokens_used   integer not null default 0,
  cost_usd      numeric(12,6) not null default 0,
  started_at    timestamptz,
  finished_at   timestamptz,
  created_at    timestamptz not null default now(),
  updated_at    timestamptz not null default now()
);
create index ix_runs_issue_id on orbit.runs (issue_id);
create index ix_runs_status_created_at on orbit.runs (status, created_at desc);

create table orbit.run_steps (
  id          uuid primary key default uuidv7(),
  run_id      uuid not null references orbit.runs(id) on delete cascade,
  ordinal     integer not null,
  agent       text not null check (agent in ('maestro','atlas','forge','viper','pixel','helm','reviewer','sentinel')),
  title       text not null,
  status      text not null check (status in ('pending','running','succeeded','failed','skipped')),
  attempts    integer not null default 0,
  started_at  timestamptz,
  finished_at timestamptz,
  constraint ux_run_steps_run_id_ordinal unique (run_id, ordinal)
);

create table orbit.agent_messages (
  id          uuid primary key default uuidv7(),
  run_id      uuid not null references orbit.runs(id) on delete cascade,
  step_id     uuid references orbit.run_steps(id) on delete cascade,
  agent       text not null,
  role        text not null check (role in ('system','assistant','tool','user')),
  content     text not null,                       -- redacted before insert
  created_at  timestamptz not null default now()
);
create index ix_agent_messages_run_id_created_at on orbit.agent_messages (run_id, created_at);
create index ix_agent_messages_step_id on orbit.agent_messages (step_id);

create table orbit.tool_calls (
  id          uuid primary key default uuidv7(),
  step_id     uuid not null references orbit.run_steps(id) on delete cascade,
  tool        text not null,
  arguments   jsonb not null,
  exit_code   integer,
  duration_ms integer,
  created_at  timestamptz not null default now()
);
create index ix_tool_calls_step_id on orbit.tool_calls (step_id);

create table orbit.artifacts (
  id          uuid primary key default uuidv7(),
  run_id      uuid not null references orbit.runs(id) on delete cascade,
  step_id     uuid references orbit.run_steps(id) on delete set null,
  kind        text not null check (kind in ('diff','log','test_report','scan_report','plan')),
  uri         text not null,                       -- blob URI; large content lives outside Postgres
  sha256      text not null,
  created_at  timestamptz not null default now()
);
create index ix_artifacts_run_id on orbit.artifacts (run_id);
create index ix_artifacts_step_id on orbit.artifacts (step_id);

create table orbit.approvals (
  id          uuid primary key default uuidv7(),
  run_id      uuid not null references orbit.runs(id) on delete cascade,
  gate        text not null check (gate in ('plan','budget','merge')),
  decision    text not null check (decision in ('approved','rejected')),
  decided_by  text not null,                       -- Entra object id, not email
  comment     text,
  created_at  timestamptz not null default now()
);
create index ix_approvals_run_id on orbit.approvals (run_id);

create table orbit.llm_usage (
  id                uuid primary key default uuidv7(),
  run_id            uuid not null references orbit.runs(id) on delete cascade,
  step_id           uuid references orbit.run_steps(id) on delete set null,
  agent             text not null,
  provider          text not null,
  model             text not null,
  prompt_tokens     integer not null,
  completion_tokens integer not null,
  cost_usd          numeric(12,6) not null,
  latency_ms        integer not null,
  created_at        timestamptz not null default now()
);
create index ix_llm_usage_run_id on orbit.llm_usage (run_id);
create index ix_llm_usage_step_id on orbit.llm_usage (step_id);
create index ix_llm_usage_created_at on orbit.llm_usage using brin (created_at);

create table orbit.code_chunks (
  id              uuid primary key default uuidv7(),
  repo_id         uuid not null references orbit.repos(id) on delete cascade,
  commit_sha      text not null,
  path            text not null,
  start_line      integer not null,
  end_line        integer not null,
  content         text not null,
  embedding       vector(1536) not null,           -- dimension fixed once the embedding model is chosen
  embedding_model text not null,
  chunk_version   integer not null,
  created_at      timestamptz not null default now()
);
create index ix_code_chunks_repo_id_path on orbit.code_chunks (repo_id, path);
create index ix_code_chunks_embedding on orbit.code_chunks using hnsw (embedding vector_cosine_ops);
create index ix_code_chunks_content_trgm on orbit.code_chunks using gin (content gin_trgm_ops);  -- keyword half of hybrid retrieval
```

## Open questions (decide in Phase 1)
- Embedding model and dimension (`vector(1536)` is a placeholder).
- Retention for `agent_messages` / `tool_calls` (likely 90 days, then partition or purge).
