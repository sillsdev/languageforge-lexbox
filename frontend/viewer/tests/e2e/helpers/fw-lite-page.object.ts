import type {Page} from '@playwright/test';
import type {E2ETestConfig} from '../types';

/**
 * Page Object Model for FW Lite UI interactions
 */
export class FwLitePageObject {
  constructor(private page: Page, private config: E2ETestConfig) {
  }

  /**
   * Navigate to the FW Lite application
   */
  async navigateToApp(): Promise<void> {
    await this.page.goto('/');
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Wait for application to be ready
   */
  async waitForAppReady(): Promise<void> {
    // Wait for main application container to be visible
    await this.page.waitForSelector('body', {timeout: 30000});

    // Wait for any loading indicators to disappear
    const loadingIndicators = this.page.locator('.loading, [data-testid="loading"], .spinner');
    try {
      await loadingIndicators.waitFor({state: 'detached', timeout: 10000});
    } catch {
      // Loading indicators might not exist, continue
    }

    // Ensure page is interactive
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Take a screenshot for debugging
   */
  async takeDebugScreenshot(name: string): Promise<void> {
    await this.page.screenshot({
      path: `test-results/debug-${name}-${Date.now()}.png`,
      fullPage: true
    });
  }

  /**
   * Get page title for verification
   */
  async getPageTitle(): Promise<string> {
    return await this.page.title();
  }

  /**
   * Check if user is logged in
   */
  async isUserLoggedIn(): Promise<boolean> {
    const userIndicator = this.page.locator(`#${this.config.lexboxServer.hostname} .i-mdi-account-circle`).first();
    return await userIndicator.isVisible().catch(() => false);
  }

  /**
   * Get current URL for verification
   */
  getCurrentUrl(): string {
    return this.page.url();
  }
}
