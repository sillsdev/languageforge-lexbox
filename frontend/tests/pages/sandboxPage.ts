import type { Page } from '@playwright/test';
import { BasePage } from './basePage';

export class SandboxPage extends BasePage {
  constructor(page: Page) {
    super(page, page.getByRole('heading', {name: 'Sandbox'}), `/sandbox`);
  }
}
