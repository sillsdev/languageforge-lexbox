import {type Locator, type Page, expect} from '@playwright/test';

/**
 * Component object for the entry view/edit panel.
 * Provides locators and common actions for editing entry fields.
 */
export class EntryViewComponent {
  readonly container: Locator;
  readonly lexemeFormField: Locator;
  readonly addSenseButton: Locator;
  readonly menuButton: Locator;

  constructor(readonly page: Page) {
    this.container = page.locator('.entry-view, [data-entry-view]');
    this.lexemeFormField = page.locator('[style*="grid-area: lexemeForm"]');
    this.addSenseButton = page.getByRole('button', {name: /add (sense|meaning)/i});
    this.menuButton = page.locator('.i-mdi-dots-vertical');
  }

  async waitForEntryLoaded(): Promise<void> {
    await expect(this.menuButton).toBeVisible({timeout: 5000});
  }

  async waitForEntrySaved(): Promise<void> {
    await this.page.waitForTimeout(600);
    await expect(this.page.locator('.i-mdi-loading')).toHaveCount(0, {timeout: 5000});
  }

  glossFieldContainer(index = 0): Locator {
    return this.page.locator('[style*="grid-area: gloss"]').nth(index);
  }

  async getGlossInput(senseIndex = 0, writingSystem?: string): Promise<Locator> {
    const container = this.glossFieldContainer(senseIndex);
    await expect(container).toBeVisible({timeout: 5000});

    if (writingSystem) {
      const label = container.locator(`label:has-text("${writingSystem}")`);
      if (await label.count() > 0) {
        const labelFor = await label.getAttribute('for');
        if (labelFor) {
          return this.page.locator(`#${labelFor}`);
        }
      }
    }
    return container.locator('input').first();
  }

  async getLexemeInput(): Promise<Locator> {
    await expect(this.lexemeFormField).toBeVisible({timeout: 5000});
    return this.lexemeFormField.locator('input').first();
  }

  async editGloss(newValue: string, senseIndex = 0, writingSystem?: string): Promise<void> {
    const input = await this.getGlossInput(senseIndex, writingSystem);
    await expect(input).toBeVisible({timeout: 5000});
    await input.click();
    await input.press('ControlOrMeta+a');
    await input.fill(newValue);
    await input.press('Tab');
    await this.waitForEntrySaved();
  }

  async editLexemeForm(newValue: string): Promise<void> {
    const input = await this.getLexemeInput();
    await expect(input).toBeVisible({timeout: 5000});
    await input.click();
    await input.press('ControlOrMeta+a');
    await input.fill(newValue);
    await input.press('Tab');
    await this.waitForEntrySaved();
  }

  async addSense(): Promise<void> {
    await expect(this.addSenseButton).toBeVisible({timeout: 5000});
    await this.addSenseButton.click();
  }

  async getSenseCount(): Promise<number> {
    return this.page.locator('[style*="grid-area: gloss"]').count();
  }
}
