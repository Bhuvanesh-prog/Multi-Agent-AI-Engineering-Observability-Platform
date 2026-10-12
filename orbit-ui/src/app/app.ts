import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MetaApi } from './core/meta/meta-api';

@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly meta = inject(MetaApi).meta;

  /** "1.0.0+<sha>" → "1.0.0" for display. */
  protected readonly version = computed(() => this.meta.value()?.version.split('+')[0] ?? '');
}
