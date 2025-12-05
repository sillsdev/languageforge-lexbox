import {BasePage} from './basePage';
import type {Page} from '@playwright/test';

export class SandboxPage extends BasePage {
  constructor(page: Page) {
    super(page, page.getByRole('heading', {name: 'Sandbox'}), '/sandbox');
  }
}
