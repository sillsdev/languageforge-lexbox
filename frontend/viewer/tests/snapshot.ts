import {argosScreenshot, type ArgosScreenshotOptions} from '@argos-ci/playwright';
import type {Page} from '@playwright/test';

const defaultOptions: ArgosScreenshotOptions = {
  viewports: [
    {height: 720, width: 1280},
    'iphone-x'
  ]
};

export async function assertScreenshot(page: Page, name: string, options?: ArgosScreenshotOptions): Promise<void> {
  await argosScreenshot(page, name, {...defaultOptions, ...options});
}
