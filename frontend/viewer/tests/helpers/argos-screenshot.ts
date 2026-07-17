import {argosScreenshot, type ArgosScreenshotOptions} from '@argos-ci/playwright';
import type {Page} from '@playwright/test';

const defaultOptions: ArgosScreenshotOptions = {
  viewports: [
    {height: 720, width: 1280},
    'iphone-x',
  ],
  // The app version is a per-build string; hiding it keeps snapshots stable across builds.
  // The "Made with ❤️ from 🇦🇹 🇹🇭 🇺🇸" line contains a heart + regional-indicator flag emoji
  // whose glyphs depend on a color-emoji font that loads inconsistently in CI, so their width
  // (and thus the surrounding layout) churns between screenshots. Both are decorative and not
  // meaningful for visual regression. visibility:hidden preserves layout so nothing else shifts.
  argosCSS: '[data-testid="app-version"], [data-testid="made-with"] { visibility: hidden; }',
};

const marketingScreenshotSizes: Exclude<ArgosScreenshotOptions['viewports'], undefined> = [
  {width: 1024, height: 500}, // android feature graphic
  {width: 720, height: 1280},
  {preset: 'macbook-16', orientation: 'landscape'},
  {preset: 'macbook-16', orientation: 'portrait'},
  {width: 1620, height: 2160}, // ipad
  {width: 2160, height: 1620},
];

export async function assertScreenshotInBothColorSchemes(page: Page, name: string, options?: ArgosScreenshotOptions): Promise<void> {
  for (const colorScheme of ['light', 'dark'] as const) {
    await page.emulateMedia({colorScheme});
    await assertScreenshot(page, `${name}-${colorScheme}`, options);
  }
  // Reset so anything after (including Playwright's automatic end-of-test screenshot) uses the default scheme.
  await page.emulateMedia({colorScheme: null});
}

export async function assertScreenshot(page: Page, name: string, options?: ArgosScreenshotOptions): Promise<void> {
  if (process.env.MARKETING_SCREENSHOTS === 'true') {
    options = {
      ...options,
      viewports: [...defaultOptions.viewports ?? [], ...marketingScreenshotSizes],
    };
  }
  await argosScreenshot(page, name, {...defaultOptions, ...options});
}
