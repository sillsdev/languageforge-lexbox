import { expect, type Download, type Locator, type Page } from '@playwright/test';
import { BaseComponent } from './baseComponent';

const PLEASE_CONFIRM_DOWNLOAD_TEXT = 'Please confirm you have downloaded a backup';
const PLEASE_CONFIRM_PROJECT_CODE_TEXT = 'Please type the project code to confirm reset';
const I_CONFIRM_DOWNLOAD_TEXT = 'I confirm that I have downloaded a backup';
const DOWNLOAD_PROJECT_BACKUP_TEXT = 'Download project backup';
const ENTER_PROJECT_CODE_TEXT = 'Enter project code to confirm';
const PROJECT_UPLOAD_CONTROL_LABEL = 'Project zip file';
const PROJECT_UPLOAD_BUTTON_LABEL = 'Upload Project';

export class ResetProjectModal extends BaseComponent {
  get downloadProjectBackupButton(): Locator {
    return this.componentLocator.getByRole('link').filter({hasText: DOWNLOAD_PROJECT_BACKUP_TEXT});
  }

  get confirmBackupDownloadedCheckbox(): Locator {
    return this.componentLocator.locator('form#reset-form').getByRole('checkbox', {name: I_CONFIRM_DOWNLOAD_TEXT});
  }

  get confirmProjectCodeInput(): Locator {
    return this.componentLocator.locator('form#reset-form').getByLabel(ENTER_PROJECT_CODE_TEXT);
  }

  get projectUploadControl(): Locator {
    return this.componentLocator.getByLabel(PROJECT_UPLOAD_CONTROL_LABEL);
  }

  get projectUploadButton(): Locator {
    return this.componentLocator.getByRole('button', {name: PROJECT_UPLOAD_BUTTON_LABEL});
  }

  get errorNoBackupDownloaded(): Locator {
    return this.componentLocator.locator('form#reset-form').getByText(PLEASE_CONFIRM_DOWNLOAD_TEXT);
  }

  get errorProjectCodeDoesNotMatch(): Locator {
    return this.componentLocator.locator('form#reset-form').getByText(PLEASE_CONFIRM_PROJECT_CODE_TEXT);
  }

  // TODO: method that confirms which modal step (by number) is active (1, 2, 3, 4)

  constructor(page: Page) {
    super(page, page.locator('.reset-modal dialog.modal'));
  }

  async downloadProjectBackup(): Promise<Download> {
    const downloadPromise = this.page.waitForEvent('download');
    await this.downloadProjectBackupButton.click();
    return downloadPromise;
  }

  async clickNextStepButton(expectedLabel: string): Promise<void> {
    await this.componentLocator.getByRole('button', {name: expectedLabel}).click();
    await expect(this.errorNoBackupDownloaded).toBeHidden();
    await expect(this.errorProjectCodeDoesNotMatch).toBeHidden();

  }

  async confirmProjectBackupReceived(projectCode: string): Promise<void> {
    await this.confirmBackupDownloadedCheckbox.check();
    await this.confirmProjectCodeInput.fill(projectCode);
  }

  async uploadProjectZipFile(filename: string): Promise<void> {
    await this.projectUploadControl.setInputFiles(filename);
    await expect(this.projectUploadButton).toBeVisible();
    await expect(this.projectUploadButton).toBeEnabled();
    await this.projectUploadButton.click();
  }
}
