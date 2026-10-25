import {expect, test} from '@playwright/test';
import {docsScreenshot, openStory} from './docs-screenshot';

// The home screen, from the home-view stories (it needs project and login services that only the
// .NET host provides, so the story fakes them).

test('logged out: Create Example Project and Login to see projects', async ({page}) => {
  const pane = await openStory(page, 'home-home-view--logged-out');
  await expect(pane.getByText('Create Example Project')).toBeVisible();
  await expect(pane.getByRole('button', {name: 'Login to see projects'})).toBeVisible();
  await docsScreenshot(page, 'home-logged-out', pane);
});

test('logged in: projects to download', async ({page}) => {
  const pane = await openStory(page, 'home-home-view--logged-in');
  await expect(pane.getByRole('button', {name: 'Download'}).first()).toBeVisible();
  await docsScreenshot(page, 'home-logged-in', pane);
});

test('project context menu with Delete', async ({page}) => {
  const pane = await openStory(page, 'home-home-view--logged-in');
  await pane.getByRole('link', {name: /Sena 3/}).click({button: 'right'});
  await expect(page.getByRole('menuitem', {name: 'Delete'})).toBeVisible();
  // Menu renders in a portal outside the pane, so capture the whole page.
  await docsScreenshot(page, 'home-project-menu');
});
