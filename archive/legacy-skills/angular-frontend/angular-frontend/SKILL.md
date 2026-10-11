---
name: angular-frontend
description: Use when building, reviewing or refactoring Angular 17+ frontend code — components, services, routing, forms, state with signals, HTTP calls to a .NET API, and UI tests.
---

# Angular 17+ Frontend

Apply these conventions whenever writing or reviewing Angular code. If the existing codebase clearly follows a different convention, follow the codebase and mention the difference once.

## Project structure
```
src/app/
  core/          # singletons: interceptors, guards, auth, app-wide services
  shared/        # reusable dumb components, pipes, directives
  features/
    <feature>/
      components/
      services/
      models/
      <feature>.routes.ts
  app.config.ts
  app.routes.ts
```
- One feature per folder, lazy-loaded via `loadChildren` / `loadComponent`.
- No `NgModule`s in new code. Use standalone components and `provideX()` functions in `app.config.ts`.

## Components
- Always `standalone: true` (the default in 17+), `changeDetection: ChangeDetectionStrategy.OnPush`.
- Use `inject()` rather than constructor injection.
- Use the signal APIs: `input()`, `input.required()`, `output()`, `model()`, `viewChild()`.
- Use the new control flow: `@if`, `@for (item of items; track item.id)`, `@switch`, `@defer`. Never use `*ngIf` / `*ngFor` in new code.
- Smart (container) components fetch data. Dumb (presentational) components only take inputs and emit outputs.
- Keep templates thin. Move logic into `computed()` signals or methods.
- Keep a component file under roughly 200 lines. Split it when it grows past that.

## State
- Local state: `signal()`, `computed()`, `effect()` (use `effect` sparingly, only for side effects).
- Convert observables with `toSignal()`. Convert back with `toObservable()` only when RxJS operators are needed.
- Feature state: a service with private writable signals and public readonly `computed()`s. Reach for NgRx SignalStore only if state spans multiple features.
- Never mutate signal values in place. Use `.set()` or `.update(x => ({...x, ...}))`.

## HTTP and API
- One API service per backend resource. Return typed `Observable<T>`.
- Define DTO interfaces in `models/` that mirror the .NET DTOs (camelCase).
- Base URL comes from `environment.ts`. Never hard-code it.
- Use functional interceptors (`HttpInterceptorFn`) for auth tokens, error handling (map ProblemDetails to user messages), and a loading indicator.
- Unsubscribe with `takeUntilDestroyed()` or use `toSignal`. No manual `subscribe` without cleanup.

## Forms
- Use typed Reactive Forms (`FormGroup<{...}>`, `nonNullable: true`).
- Mirror backend validation rules on the client, but treat the backend as the source of truth. Display server ProblemDetails `errors` against the matching fields.

## Routing
- Use functional guards and resolvers (`CanActivateFn`, `ResolveFn`).
- Bind route params to inputs with `withComponentInputBinding()`.

## Styling and accessibility
- Use SCSS with component-scoped styles. Put design tokens in CSS variables.
- Use semantic HTML, `aria-*` on custom controls, keyboard support, and visible focus states.

## Testing
- Unit test services and component logic with Jest or Jasmine using `TestBed` and `provideHttpClientTesting()`.
- Test behaviour, not implementation: query the DOM by role or text.
- Write at least one test per component covering the happy path and one edge case.

## Review checklist
- [ ] OnPush, standalone, `inject()`, signal inputs
- [ ] New control flow with `track`
- [ ] No leaked subscriptions
- [ ] Typed forms and DTOs, no `any`
- [ ] Lazy-loaded feature routes
- [ ] Errors surfaced to the user, loading states handled
- [ ] Tests added or updated

## Output
When generating code, give complete files with their paths, then a short note on what to wire up (routes, providers).
