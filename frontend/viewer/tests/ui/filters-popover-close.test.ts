import {expect, test} from '@playwright/test';

import {DemoProjectPage} from './demo-project.page';

// An open tooltip used to swallow the outside click that should close the popover.
test('filters popover closes when clicking a button whose tooltip is open', async ({page}) => {
  const projectPage = new DemoProjectPage(page);
  await projectPage.goto();
  const filtersToggle = page.getByRole('button', {name: 'Toggle filters'});
  await filtersToggle.click();
  await expect(filtersToggle).toHaveAttribute('aria-expanded', 'true');

  const newEntryButton = page.getByRole('button', {name: 'New Word'});
  await newEntryButton.hover();
  await expect(page.locator('[data-slot="tooltip-content"]')).toBeVisible();
  await newEntryButton.click();

  await expect(page.getByRole('dialog', {name: 'New Word'})).toBeVisible();
  // the modal dialog hides the page from the a11y tree, so locate the toggle by attribute
  await expect(page.locator('[aria-label="Toggle filters"]')).toHaveAttribute('aria-expanded', 'false');
});
