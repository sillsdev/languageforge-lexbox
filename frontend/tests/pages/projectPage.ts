import type { Locator, Page } from '@playwright/test';

import { AddMemberModal } from '../components/addMemberModal';
import { BasePage } from './basePage';
import { DeleteProjectModal } from '../components/deleteProjectModal';
import { ResetProjectModal } from '../components/resetProjectModal';
import { ViewerPage } from './viewerPage';

export class ProjectPage extends BasePage {
  get moreSettingsDiv(): Locator { return this.page.locator('.collapse').filter({ hasText: 'More settings' }); }
  get deleteProjectButton(): Locator { return this.moreSettingsDiv.getByRole('button', {name: 'Delete project'}); }
  get resetProjectButton(): Locator { return this.moreSettingsDiv.getByRole('button', {name: 'Reset project'}); }
  get verifyRepoButton(): Locator { return this.moreSettingsDiv.getByRole('button', {name: 'Verify repository'}); }
  get addMemberButton(): Locator { return this.page.getByRole('button', {name: 'Add/Invite Member'}); }
  get browseButton(): Locator { return this.page.getByRole('link', {name: 'Browse'}); }

  constructor(page: Page, private name: string, private code: string) {
    super(page, page.getByRole('heading', {name: `Project: ${name}`}), `/project/${code}`);
  }

  openMoreSettings(): Promise<void> {
    return this.moreSettingsDiv.getByRole('checkbox').check();
  }

  async clickAddMember(): Promise<AddMemberModal> {
    await this.addMemberButton.click();
    return new AddMemberModal(this.page).waitFor()
  }

  async clickDeleteProject(): Promise<DeleteProjectModal> {
    await this.openMoreSettings();
    await this.deleteProjectButton.click();
    return await new DeleteProjectModal(this.page).waitFor();
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

  async clickBrowseInViewer(): Promise<ViewerPage> {
    const viewerTabPromise = this.page.context().waitForEvent('page')
    await this.browseButton.click();
    const viewerTab = await viewerTabPromise;
    return new ViewerPage(viewerTab, this.name, this.code).waitFor();
  }
}
