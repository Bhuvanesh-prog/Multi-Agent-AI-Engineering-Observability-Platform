import { render, screen } from '@testing-library/angular';
import { RunsPage } from './runs-page';

describe('RunsPage', () => {
  it('shows the empty state when there are no runs', async () => {
    await render(RunsPage);

    expect(screen.getByRole('heading', { level: 1, name: 'Runs' })).toBeTruthy();
    expect(screen.getByRole('heading', { name: 'No runs yet' })).toBeTruthy();
  });
});
