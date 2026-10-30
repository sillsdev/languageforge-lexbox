import {expect, type Locator, type Page} from '@playwright/test';
import {storybookPort} from './playwright.config';
import path from 'node:path';

// Screenshots land in the docs site's static folder; Docusaurus serves docs/static at the site root,
// so `fw-lite/<name>.png` here is `/img/fw-lite/<name>.png` in a doc page.
export const outputDir = path.resolve(import.meta.dirname, '../../../../docs/static/img/fw-lite');

export type ColorScheme = 'light' | 'dark';

// Per-build and environment-dependent content that would churn the images between runs.
const hideChurnCss = `
  [data-testid="app-version"], [data-testid="made-with"] { visibility: hidden !important; }
  /* dev-only Storybook shortcut in the home app bar */
  a:has(> img[alt="Storybook icon"]), a:has(img[alt="Storybook icon"]) { display: none !important; }
  *, *::before, *::after { caret-color: transparent !important; }
`;

export async function prepareForScreenshot(page: Page, target?: Locator): Promise<void> {
  await page.addStyleTag({content: hideChurnCss});
  await page.waitForFunction(() => document.fonts.ready);
  // Spinners/skeletons inside what we're capturing mean it hasn't finished rendering.
  const scope = target ?? page.locator('body');
  await expect(scope.locator('.i-mdi-loading')).toHaveCount(0);
  await expect(scope.locator('.animate-pulse')).toHaveCount(0);
}

function fileName(name: string, colorScheme: ColorScheme): string {
  return path.join(outputDir, colorScheme === 'light' ? `${name}.png` : `${name}-dark.png`);
}

/**
 * Captures `name.png` (light) and `name-dark.png` (dark) so a doc page can use Docusaurus'
 * `<ThemedImage sources={{light: ..., dark: ...}} />`. `target` crops to one element (default: the page).
 */
export async function docsScreenshot(page: Page, name: string, target?: Locator): Promise<void> {
  for (const colorScheme of ['light', 'dark'] as const) {
    await page.emulateMedia({colorScheme});
    await prepareForScreenshot(page, target);
    const options = {path: fileName(name, colorScheme), animations: 'disabled', caret: 'hide'} as const;
    if (target) await target.screenshot(options);
    else await page.screenshot(options);
  }
  await page.emulateMedia({colorScheme: null});
}

/**
 * Opens a story in the Storybook iframe (no Storybook chrome) and returns the pane the story renders
 * into, for cropped component shots. `id` is the story id from the Storybook URL, e.g.
 * `home-server--server` (title `home/server`, story `Server`).
 */
export async function openStory(page: Page, id: string): Promise<Locator> {
  await page.goto(`http://localhost:${storybookPort}/iframe.html?id=${id}&viewMode=story`);
  const root = page.locator('#storybook-root');
  await expect(root).toBeVisible();
  // FWLiteDecorator adds a fixed theme/mode picker in the corner and a resizable "value" side pane;
  // neither is part of the component being documented.
  await page.addStyleTag({content: '#storybook-root .fixed.bottom-4.right-4 { display: none !important; }'});
  const storyPane = root.locator('[data-pane]').first();
  await expect(storyPane).toBeVisible();
  return storyPane;
}
