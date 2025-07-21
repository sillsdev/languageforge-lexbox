import papi, { logger } from '@papi/backend';
import type { MandatoryProjectDataTypes } from '@papi/core';
import type { IBaseProjectDataProvider } from 'papi-shared-types';

export class WebViewProjectSettings {
  private readonly pdp: IBaseProjectDataProvider<MandatoryProjectDataTypes>;
  readonly projectId: string;

  private constructor(pdp: IBaseProjectDataProvider<MandatoryProjectDataTypes>, projectId: string) {
    this.pdp = pdp;
    this.projectId = projectId;
  }

  static async getProjectIdFromWebViewId(webViewId: string): Promise<string | undefined> {
    const webViewDef = await papi.webViews
      .getOpenWebViewDefinition(webViewId)
      .catch((e) => void logger.error('Error getting web view definition:', e));
    if (!webViewDef?.projectId) {
      logger.warn(`No projectId found for web view '${webViewId}'`);
      return undefined;
    }
    return webViewDef.projectId;
  }

  static async createFromProjectId(projectId: string): Promise<WebViewProjectSettings | undefined> {
    if (!projectId) return;
    const pdp = await papi.projectDataProviders.get('platform.base', projectId);
    return new WebViewProjectSettings(pdp, projectId);
  }

  static async createFromWebViewId(webViewId: string): Promise<WebViewProjectSettings | undefined> {
    const projectId = await WebViewProjectSettings.getProjectIdFromWebViewId(webViewId);
    if (!projectId) return;
    const pdp = await papi.projectDataProviders.get('platform.base', projectId);
    return new WebViewProjectSettings(pdp, projectId);
  }

  async getFwDictionaryCode(): Promise<string> {
    return await this.pdp.getSetting('fw-lite-extension.fwDictionaryCode');
  }

  async getProjectName(): Promise<string> {
    return await this.pdp.getSetting('platform.name');
  }

  async setFwDictionaryCode(dictionaryCode: string): Promise<void> {
    logger.info(`Selecting FieldWorks dictionary '${dictionaryCode}'`);
    await this.pdp.setSetting('fw-lite-extension.fwDictionaryCode', dictionaryCode);
  }
}
