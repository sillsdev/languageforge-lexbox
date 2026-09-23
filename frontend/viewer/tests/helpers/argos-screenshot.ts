import {argosScreenshot, type ArgosScreenshotOptions} from '@argos-ci/playwright';
import {test, type Page} from '@playwright/test';
import {MOBILE_BREAKPOINT} from '../../src/css-breakpoints';

// The viewports every UI snapshot is captured at. We resize the page ourselves (rather than using
// Argos's `viewports` option) so we can wait for the responsive layout to actually reflow after each
// resize — see waitForResponsiveLayout.
const baseViewports = [
  {width: 1280, height: 720},
  {width: 375, height: 812}, // iphone-x
];

// Extra sizes captured only for the opt-in marketing run (chromium). These include presets that
// Argos resolves, so that path keeps using Argos's own viewport handling.
const marketingScreenshotSizes: Exclude<ArgosScreenshotOptions['viewports'], undefined> = [
  {width: 1024, height: 500}, // android feature graphic
  {width: 720, height: 1280},
  {preset: 'macbook-16', orientation: 'landscape'},
  {preset: 'macbook-16', orientation: 'portrait'},
  {width: 1620, height: 2160}, // ipad
  {width: 2160, height: 1620},
];

const commonOptions: ArgosScreenshotOptions = {
  // The app version is a per-build string; hiding it keeps snapshots stable across builds.
  // The "Made with ❤️ from 🇦🇹 🇹🇭 🇺🇸" line contains a heart + regional-indicator flag emoji
  // whose glyphs depend on a color-emoji font that loads inconsistently in CI, so their width
  // (and thus the surrounding layout) churns between screenshots. Both are decorative and not
  // meaningful for visual regression. visibility:hidden preserves layout so nothing else shifts.
  argosCSS: '[data-testid="app-version"], [data-testid="made-with"] { visibility: hidden; }',
};

// Snapshots are captured at multiple widths by resizing the live page. WebKit — notably on the Linux
// CI runner — doesn't reflow the matchMedia-driven master-detail pane switch as promptly as Chromium,
// so capturing right after the resize can photograph the desktop layout at mobile width. Wait until
// the DOM reflects the width: the split-pane resizer ([data-pane-resizer]) is rendered only in the
// desktop layout (`{#if !IsMobile.value}` in MasterDetailView.svelte).
async function waitForResponsiveLayout(page: Page, width: number): Promise<void> {
  const wantsResizer = width >= MOBILE_BREAKPOINT;
  await page
    .waitForFunction(
      (want) => (document.querySelector('[data-pane-resizer]') !== null) === want,
      wantsResizer,
      {timeout: 10_000},
    )
    .catch(() => {
      // Fall through and capture whatever is rendered rather than failing the run; a layout that
      // never settled shows up as a visual diff in Argos for review.
    });
}

export async function assertScreenshotInBothColorSchemes(page: Page, name: string, options?: ArgosScreenshotOptions): Promise<void> {
  for (const colorScheme of ['light', 'dark'] as const) {
    await page.emulateMedia({colorScheme});
    await assertScreenshot(page, `${name}-${colorScheme}`, options);
  }
  // Reset so anything after (including Playwright's automatic end-of-test screenshot) uses the default scheme.
  await page.emulateMedia({colorScheme: null});
}

export async function assertScreenshot(page: Page, name: string, options?: ArgosScreenshotOptions): Promise<void> {
  // chromium keeps bare names (its existing Argos baselines are the primary set); other browsers
  // (e.g. webkit) get a browser-suffixed name so they add a separate baseline instead of colliding.
  const projectName = test.info().project.name;
  const screenshotName = projectName === 'chromium' ? name : `${name}-${projectName}`;

  if (process.env.MARKETING_SCREENSHOTS === 'true') {
    // Marketing runs (opt-in, chromium) capture many preset sizes; let Argos drive those viewports.
    await argosScreenshot(page, screenshotName, {
      ...commonOptions,
      ...options,
      viewports: [...baseViewports, ...marketingScreenshotSizes],
    });
    return;
  }

  const originalViewport = page.viewportSize();
  for (const {width, height} of baseViewports) {
    await page.setViewportSize({width, height});
    await waitForResponsiveLayout(page, width);
    // Argos appends " vw-<width>" only when it drives the viewports; we drive them, so append it
    // ourselves to keep the same snapshot names (and thus the existing chromium baselines).
    await argosScreenshot(page, `${screenshotName} vw-${width}`, {...commonOptions, ...options, viewports: undefined});
  }
  if (originalViewport) await page.setViewportSize(originalViewport);
}
