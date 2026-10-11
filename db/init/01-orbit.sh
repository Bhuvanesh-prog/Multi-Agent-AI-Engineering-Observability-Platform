#!/usr/bin/env bash
# 🗺️ Atlas — first-boot setup for the local Orbit database. Safe: runs only on an empty data volume.
# Roles: orbit_owner owns schemas and runs migrations; orbit_app gets DML only.
# Passwords come from the container environment (.env), never from this file.
set -euo pipefail

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
  -v owner_pw="$ORBIT_OWNER_PASSWORD" -v app_pw="$ORBIT_APP_PASSWORD" <<'SQL'
CREATE EXTENSION IF NOT EXISTS vector;
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE ROLE orbit_owner LOGIN PASSWORD :'owner_pw';
CREATE ROLE orbit_app   LOGIN PASSWORD :'app_pw';

REVOKE ALL ON DATABASE orbit FROM PUBLIC;
GRANT CONNECT, TEMPORARY ON DATABASE orbit TO orbit_owner, orbit_app;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;

-- orbit: control-plane tables (EF Core migrations). agent: LangGraph checkpoints.
CREATE SCHEMA orbit AUTHORIZATION orbit_owner;
CREATE SCHEMA agent AUTHORIZATION orbit_owner;

GRANT USAGE ON SCHEMA orbit, agent TO orbit_app;
ALTER DEFAULT PRIVILEGES FOR ROLE orbit_owner IN SCHEMA orbit, agent
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO orbit_app;
ALTER DEFAULT PRIVILEGES FOR ROLE orbit_owner IN SCHEMA orbit, agent
  GRANT USAGE, SELECT ON SEQUENCES TO orbit_app;

ALTER ROLE orbit_owner SET search_path = orbit, public;
ALTER ROLE orbit_app   SET search_path = orbit, public;
ALTER ROLE orbit_app   SET statement_timeout = '30s';
ALTER ROLE orbit_app   SET idle_in_transaction_session_timeout = '60s';
SQL
