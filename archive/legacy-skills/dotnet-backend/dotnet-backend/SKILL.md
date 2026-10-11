---
name: dotnet-backend
description: Use when building, reviewing or refactoring .NET 8 backend code — Web APIs (controllers or minimal APIs), service/repository layers, DTOs and validation, auth, error handling, logging, and xUnit tests.
---

# .NET 8 Backend

Apply these conventions whenever writing or reviewing ASP.NET Core code. If the existing codebase clearly follows a different convention, follow the codebase and mention the difference once.

## Solution structure (Clean Architecture, kept pragmatic)
```
src/
  App.Api/            # controllers/endpoints, middleware, Program.cs, DI wiring
  App.Application/    # use cases, services, DTOs, validators, interfaces
  App.Domain/         # entities, value objects, enums, domain rules (no dependencies)
  App.Infrastructure/ # EF Core DbContext, repositories, external clients
tests/
  App.UnitTests/
  App.IntegrationTests/
```
- Dependencies point inward: Api -> Application -> Domain. Infrastructure implements Application interfaces.
- For a small service, collapse into one project with folders. Don't add layers that carry no logic.

## Language and style
- `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, file-scoped namespaces, implicit usings.
- Primary constructors for DI in services; `record` for DTOs; `required` / `init` for immutable models.
- Async all the way: `async Task<T>`, pass `CancellationToken` from the endpoint down to EF Core. Never `.Result` or `.Wait()`.
- Name methods by intent: `GetPolicyByIdAsync`, not `Get`.

## API design
- RESTful routes: `api/v1/policies/{id}`; plural nouns; versioning via `Asp.Versioning`.
- Return `ActionResult<T>` / `Results<Ok<T>, NotFound>` (typed results for minimal APIs).
- Status codes: 200 read, 201 + `Location` on create, 204 on delete/update without body, 400 validation, 401/403 auth, 404 missing, 409 conflict.
- Never return entities. Map to response DTOs (manual mapping or Mapster; avoid heavy AutoMapper magic).
- Paginate list endpoints: `?page=1&pageSize=20`, cap `pageSize`, return `{ items, totalCount, page, pageSize }`.

## Validation and errors
- FluentValidation for request DTOs, run via an endpoint filter or action filter.
- Global exception handling with `IExceptionHandler` + `AddProblemDetails()`; return RFC 7807 ProblemDetails.
- Map domain exceptions (`NotFoundException`, `ConflictException`) to status codes in one place. Never leak stack traces outside Development.
- Use a Result type for expected failures in the Application layer; exceptions for the truly exceptional.

## Security
- JWT bearer auth (or Entra ID via `Microsoft.Identity.Web`); policy-based authorization (`[Authorize(Policy = "...")]`).
- Secrets from user-secrets locally and Azure Key Vault in deployed environments. Never in `appsettings.json` or source.
- Explicit CORS allow-list for the Angular origin; rate limiting via `AddRateLimiter` on public endpoints.
- Validate ownership on every resource access (no IDOR).

## Configuration and DI
- Strongly typed options: `services.AddOptions<SmtpOptions>().BindConfiguration("Smtp").ValidateDataAnnotations().ValidateOnStart();`
- Lifetimes: DbContext and repositories scoped; stateless helpers singleton; `HttpClient` via `IHttpClientFactory` with Polly resilience (`AddStandardResilienceHandler`).
- Group registrations in extension methods per layer: `AddApplication()`, `AddInfrastructure(config)`.

## Logging and observability
- Serilog or built-in logging with structured templates: `logger.LogInformation("Policy {PolicyId} renewed", id)` — never string interpolation.
- Add correlation IDs; health checks at `/health` (include DB check); OpenTelemetry for traces when available.
- Don't log PII, tokens, or full request bodies.

## Background work
- `BackgroundService` for simple recurring jobs; Hangfire or Quartz when you need persistence/retries/dashboards.

## Testing
- Unit: xUnit + FluentAssertions + NSubstitute (or Moq); test Application services with mocked interfaces.
- Integration: `WebApplicationFactory<Program>` + Testcontainers PostgreSQL; test real HTTP + DB, not the in-memory provider.
- Name tests `Method_Scenario_ExpectedResult`. One behaviour per test, Arrange/Act/Assert.

## Review checklist
- [ ] CancellationToken threaded through, no sync-over-async
- [ ] DTOs in/out, no entities exposed
- [ ] Validation + ProblemDetails for errors
- [ ] Authorization and ownership checks
- [ ] No secrets in config/source
- [ ] Structured logs, no PII
- [ ] Unit + integration tests added/updated

## Output
When generating code, give complete files with their paths and the DI/Program.cs registrations needed to wire them up.
