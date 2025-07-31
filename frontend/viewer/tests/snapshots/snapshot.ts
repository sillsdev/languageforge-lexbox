import {argosScreenshot, type ArgosScreenshotOptions} from '@argos-ci/playwright';
import type {Page} from '@playwright/test';

const defaultOptions: ArgosScreenshotOptions = {
  viewports: [
    { height: 720, width: 1280 },
    'iphone-x'
  ],
};

const marketingScreenshotSizes: Exclude<ArgosScreenshotOptions['viewports'], undefined> = [
  { width: 1024, height: 500 },//android feature graphic
  { width: 720, height: 1280 },
  { preset: 'macbook-16', orientation: 'landscape' },
  { preset: 'macbook-16', orientation: 'portrait' },
  { width: 1620, height: 2160 },//ipad
  { width: 2160, height: 1620 }
];

export async function assertScreenshot(page: Page, name: string, options?: ArgosScreenshotOptions): Promise<void> {
  if (process.env.MARKETING_SCREENSHOTS === 'true') {
    options = {
      ...options,
      viewports: [...defaultOptions.viewports ?? [], ...marketingScreenshotSizes]
    };
  }
  await argosScreenshot(page, name, {...defaultOptions, ...options});
}
