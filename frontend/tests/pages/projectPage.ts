import type { Page } from '@playwright/test';
import { BasePage } from './basePage';

export class ProjectPage extends BasePage {
  constructor(page: Page, name: string, code: string) {
    super(page, page.locator(`.breadcrumbs :text('${name}')`), `/project/${code}`);
  }
}
