import type { Locator, Page } from '@playwright/test';

import { AddMemberModal } from '../components/addMemberModal';
import { BasePage } from './basePage';
import { ResetProjectModal } from '../components/resetProjectModal';

export class ProjectPage extends BasePage {
  get moreSettingsDiv(): Locator { return this.page.locator('.collapse').filter({ hasText: 'More settings' }); }
  get deleteProjectButton(): Locator { return this.moreSettingsDiv.getByRole('button', {name: 'Delete project'}); }
  get resetProjectButton(): Locator { return this.moreSettingsDiv.getByRole('button', {name: 'Reset project'}); }
  get verifyRepoButton(): Locator { return this.moreSettingsDiv.getByRole('button', {name: 'Verify repository'}); }
  get addMemberButton(): Locator { return this.page.getByRole('button', {name: 'Add/Invite Member'}); }

  constructor(page: Page, name: string, code: string) {
    super(page, page.locator(`.breadcrumbs :text('${name}')`), `/project/${code}`);
  }

  openMoreSettings(): Promise<void> {
    return this.moreSettingsDiv.getByRole('checkbox').check();
  }

  async clickAddMember(): Promise<AddMemberModal> {
    await this.addMemberButton.click();
    return new AddMemberModal(this.page).waitFor()
  }

  async clickDeleteProject(): Promise<void> {
    await this.openMoreSettings();
    await this.deleteProjectButton.click();
  }

  async clickResetProject(): Promise<ResetProjectModal> {
    await this.openMoreSettings();
    await this.resetProjectButton.click();
    return new ResetProjectModal(this.page).waitFor()
  }

  async clickVerifyRepo(): Promise<void> {
    await this.openMoreSettings();
    await this.verifyRepoButton.click();
  }
}
