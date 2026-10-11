import { Component } from '@angular/core';

/** Phase 0 placeholder: the live run list arrives with the control plane's runs API in Phase 1. */
@Component({
  selector: 'app-runs-page',
  template: `
    <h1>Runs</h1>
    <section class="empty" aria-labelledby="runs-empty-title">
      <h2 id="runs-empty-title">No runs yet</h2>
      <p>File an issue on a connected repo and Maestro will plan it here.</p>
    </section>
  `,
  styles: `
    .empty {
      border: 1px dashed var(--orbit-border);
      border-radius: var(--orbit-radius);
      padding: 2rem;
      text-align: center;
      color: var(--orbit-muted);
    }
    .empty h2 {
      margin: 0 0 0.5rem;
      color: var(--orbit-fg);
      font-size: 1.125rem;
    }
  `,
})
export class RunsPage {}
