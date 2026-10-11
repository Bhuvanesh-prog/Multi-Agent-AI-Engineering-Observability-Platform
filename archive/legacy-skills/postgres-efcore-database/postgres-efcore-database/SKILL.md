---
name: postgres-efcore-database
description: Use when designing PostgreSQL schemas, writing or reviewing EF Core models/configurations/migrations, writing SQL or LINQ queries, tuning slow queries, adding indexes, or planning data migrations.
---

# PostgreSQL + EF Core 8

Apply these conventions whenever working on the data layer. If the existing schema clearly follows a different convention, follow it and mention the difference once.

## Naming
- snake_case for tables and columns via `EFCore.NamingConventions` (`UseSnakeCaseNamingConvention()`), so SQL stays unquoted.
- Plural table names (`policies`), singular entity classes (`Policy`).
- Index names: `ix_<table>_<cols>`; unique: `ux_<table>_<cols>`; FK: `fk_<table>_<ref_table>`.

## Schema design
- Primary keys: `uuid` (generate with `Guid.CreateVersion7()` in .NET 9+ or a UUIDv7 library; otherwise `bigint generated always as identity`). Be consistent per database.
- Timestamps: `timestamptz` always, stored in UTC (`created_at`, `updated_at`). Never `timestamp without time zone`.
- Money: `numeric(18,2)`, never float. Text: `text` with a check constraint or `varchar(n)` only when a real limit exists.
- Enums: store as `text` with a check constraint (or EF value conversion to string) — easier to evolve than native PG enums.
- Use `jsonb` for genuinely schemaless data only; anything you filter on often deserves a real column.
- Every FK gets an index. Choose `ON DELETE` deliberately (default restrict; cascade only for owned children).
- Soft delete (`deleted_at timestamptz null`) only when the business needs it; pair with a global query filter and a partial index.
- Optimistic concurrency with PostgreSQL `xmin` (`.IsRowVersion()` on a `uint Version` property mapped to `xmin`).

## EF Core configuration
- One `IEntityTypeConfiguration<T>` per entity in `Infrastructure/Persistence/Configurations`; apply with `ApplyConfigurationsFromAssembly`.
- No data annotations on domain entities — configure in Fluent API.
- Set `updated_at` / `created_at` in a `SaveChangesInterceptor`, not in each service.
- Register with `AddDbContextPool` and `EnableRetryOnFailure()` via Npgsql.

## Querying
- Reads: `AsNoTracking()`, project with `.Select(x => new Dto(...))` — never load full entity graphs to return a DTO.
- Avoid N+1: project, or use `Include` deliberately; use `AsSplitQuery()` when including multiple collections.
- Always paginate (`Skip/Take` for small offsets; keyset pagination `WHERE (created_at, id) < (@c, @id)` for large tables).
- Bulk operations: `ExecuteUpdateAsync` / `ExecuteDeleteAsync` instead of load-modify-save loops.
- Raw SQL only through `FromSql($"...")` / `SqlQuery<T>` with interpolated parameters — never string concatenation.
- Use compiled queries only for proven hot paths.

## Indexing and performance
- Index for the actual WHERE / JOIN / ORDER BY patterns; composite index column order = equality columns first, then range/sort.
- Partial indexes for common filters (`WHERE deleted_at IS NULL`, `WHERE status = 'active'`).
- GIN indexes for `jsonb` containment and full-text (`tsvector`); `pg_trgm` for `ILIKE '%x%'` search.
- Diagnose with `EXPLAIN (ANALYZE, BUFFERS)`; look for Seq Scans on large tables, bad row estimates, and sorts spilling to disk.
- Enable `pg_stat_statements` and review top queries by total time.
- Log slow EF queries in dev: `LogTo(..., LogLevel.Information)` with `EnableSensitiveDataLogging()` only locally.

## Migrations
- One migration per logical change, named by intent: `AddRenewalDateToPolicies`.
- Always review the generated migration before committing; check for accidental drops/renames (EF sees renames as drop + add — fix with `RenameColumn`).
- Deploy with idempotent scripts (`dotnet ef migrations script --idempotent`) or bundles (`dotnet ef migrations bundle`) from the pipeline — never `Database.Migrate()` at app startup in production.
- Zero-downtime pattern (expand / contract): add nullable column → backfill in batches → deploy code using it → add NOT NULL/constraint → drop old column in a later release.
- Create indexes on large tables with `CREATE INDEX CONCURRENTLY` via `migrationBuilder.Sql(...)` and `suppressTransaction: true`.
- Never edit a migration that has already run in a shared environment; add a new one.

## Transactions
- `SaveChangesAsync` is already a transaction; use `BeginTransactionAsync` only across multiple saves or with raw SQL.
- With retry-on-failure, wrap manual transactions in `Database.CreateExecutionStrategy().ExecuteAsync(...)`.

## Security and ops
- App connects as a least-privilege role (DML only); migrations run as a separate owner role.
- Connection strings from Key Vault / environment, with `SSL Mode=Require` in Azure.
- Back up before destructive migrations; test restores, not just backups.

## Review checklist
- [ ] snake_case, timestamptz, numeric for money
- [ ] FKs indexed, delete behaviour chosen deliberately
- [ ] Reads are AsNoTracking + projected + paginated
- [ ] No N+1, no string-built SQL
- [ ] Migration reviewed; no accidental drops; expand/contract for breaking changes
- [ ] Large-table indexes created concurrently

## Output
When generating schema changes, give the entity, its configuration class, the migration command, and the key SQL the migration will produce.
