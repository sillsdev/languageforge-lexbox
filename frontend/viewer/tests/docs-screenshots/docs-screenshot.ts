import {expect, type Locator, type Page} from '@playwright/test';
import {storybookPort} from './playwright.config';
import path from 'node:path';

// Docusaurus serves docs/site/static at the site root, so these are `/img/fw-lite/<name>.png` in a page.
export const outputDir = path.resolve(import.meta.dirname, '../../../../docs/site/static/img/fw-lite');

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

/** Opening a word or dialog focuses something, and a focus ring reads as "type here" in a screenshot. */
export async function blurFocusedField(page: Page, focused = page.locator(':focus')): Promise<void> {
  await expect(focused).toHaveCount(1);
  await page.evaluate(() => (document.activeElement as HTMLElement).blur());
}

function fileName(name: string, colorScheme: ColorScheme): string {
  return path.join(outputDir, colorScheme === 'light' ? `${name}.png` : `${name}-dark.png`);
}

/**
 * Captures `name.png` (light) and `name-dark.png` (dark) so a doc page can use Docusaurus'
 * `<ThemedImage sources={{light: ..., dark: ...}} />`. `target` crops to one element, or to the box
 * around several (plus a margin); default is the whole window.
 */
export async function docsScreenshot(page: Page, name: string, target?: Locator | Locator[]): Promise<void> {
  for (const colorScheme of ['light', 'dark'] as const) {
    await page.emulateMedia({colorScheme});
    await prepareForScreenshot(page, Array.isArray(target) ? undefined : target);
    const options = {path: fileName(name, colorScheme), animations: 'disabled', caret: 'hide'} as const;
    if (Array.isArray(target)) await page.screenshot({...options, clip: await boxAround(page, target)});
    else if (target) await target.screenshot(options);
    else await page.screenshot(options);
  }
  await page.emulateMedia({colorScheme: null});
}

async function boxAround(page: Page, targets: Locator[]) {
  const margin = 12;
  const boxes = await Promise.all(targets.map(async t => (await t.boundingBox())!));
  const viewport = page.viewportSize()!;
  const x = Math.max(0, Math.min(...boxes.map(b => b.x)) - margin);
  const y = Math.max(0, Math.min(...boxes.map(b => b.y)) - margin);
  const right = Math.min(viewport.width, Math.max(...boxes.map(b => b.x + b.width)) + margin);
  const bottom = Math.min(viewport.height, Math.max(...boxes.map(b => b.y + b.height)) + margin);
  return {x, y, width: right - x, height: bottom - y};
}

/**
 * Opens a story in the Storybook iframe (no Storybook chrome) and returns the pane the story renders
 * into, for cropped component shots. `id` is the story id from the Storybook URL, e.g.
 * `home-server--server` (title `home/server`, story `Server`).
 */
export async function openStory(page: Page, id: string): Promise<Locator> {
  await page.goto(`http://localhost:${storybookPort}/iframe.html?id=${id}&viewMode=story`);
  const root = page.locator('#storybook-root');
  // Not toBeVisible: a story that only renders a dialog leaves the root empty (the dialog is portaled).
  await expect(root).not.toBeEmpty();
  // FWLiteDecorator adds a fixed theme/mode picker in the corner and a resizable "value" side pane;
  // neither is part of the component being documented.
  await page.addStyleTag({content: '#storybook-root .fixed.bottom-4.right-4 { display: none !important; }'});
  return root.locator('[data-pane]').first();
}
