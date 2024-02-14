import type { Page } from '@playwright/test';
import { BasePage } from './basePage';

export class RegisterPage extends BasePage {
  constructor(page: Page) {
    super(page, page.getByRole('heading', {name: 'Register'}), `/register`);
  }

  async fillForm(name: string, email: string, password: string): Promise<void> {
    await this.page.getByLabel('Name').fill(name);
    await this.page.getByLabel('Email').fill(email);
    await this.page.getByLabel('Password').fill(password);
  }

  async submit(): Promise<void> {
    await this.page.getByRole('button', {name: 'Register'}).click();
  }
}
