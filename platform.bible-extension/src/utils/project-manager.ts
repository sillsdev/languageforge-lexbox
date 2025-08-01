import papi, { logger } from '@papi/backend';
import type { MandatoryProjectDataTypes } from '@papi/core';
import type { OpenWebViewOptionsWithProjectId, WebViewIds, WebViewType } from 'fw-lite-extension';
import type { IBaseProjectDataProvider } from 'papi-shared-types';
// eslint-disable-next-line no-restricted-imports
import type { Layout } from 'shared/models/docking-framework.model';
import { ProjectSettingKey } from '../types/enums';

export class ProjectManager {
  readonly projectId: string;
  private dataProvider?: IBaseProjectDataProvider<MandatoryProjectDataTypes>;
  private fwDictionaryCode?: string;
  private languageTag?: string;
  private name?: string;
  private readonly webViewIds: WebViewIds = {};

  constructor(projectId: string) {
    this.projectId = projectId;
  }

  static async getFwDictionaryCode(projectId: string): Promise<string | undefined> {
    return await new ProjectManager(projectId).getFwDictionaryCode();
  }

  clearSettingsCache(): void {
    this.fwDictionaryCode = undefined;
    this.languageTag = undefined;
    this.name = undefined;
  }

  async getFwDictionaryCode(): Promise<string | undefined> {
    this.fwDictionaryCode ??= await (
      await this.getDataProvider()
    )?.getSetting(ProjectSettingKey.FwDictionaryCode);
    logger.info(`Setting '${ProjectSettingKey.FwDictionaryCode}': ${this.fwDictionaryCode}`);
    return this.fwDictionaryCode;
  }

  async setFwDictionaryCode(dictionaryCode: string): Promise<void> {
    if ((await this.getFwDictionaryCode()) === dictionaryCode) return;
    const dataProvider = await this.getDataProvider();
    if (dataProvider?.setSetting(ProjectSettingKey.FwDictionaryCode, dictionaryCode)) {
      this.fwDictionaryCode = dictionaryCode;
    }
    logger.info(`Setting '${ProjectSettingKey.FwDictionaryCode}': ${this.fwDictionaryCode}`);
  }

  async getLanguageTag(): Promise<string | undefined> {
    this.languageTag ??= await (
      await this.getDataProvider()
    )?.getSetting(ProjectSettingKey.ProjectLanguageTag);
    logger.info(`Setting '${ProjectSettingKey.ProjectLanguageTag}': ${this.languageTag}`);
    return this.languageTag;
  }

  async getName(): Promise<string | undefined> {
    this.name ??= await (await this.getDataProvider())?.getSetting(ProjectSettingKey.ProjectName);
    logger.info(`Setting '${ProjectSettingKey.ProjectName}': ${this.name}`);
    return this.name;
  }

  async getNameOrId(): Promise<string | undefined> {
    return (await this.getName()) || this.projectId;
  }

  async openWebView(
    webViewType: WebViewType,
    layout?: Layout,
    options?: OpenWebViewOptionsWithProjectId,
  ): Promise<boolean> {
    const existingId = this.webViewIds[webViewType];
    const newOptions = { ...options, existingId, projectId: this.projectId };
    logger.info(`Opening ${webViewType} WebView for project ${this.projectId}`);
    logger.info(`WebView options: ${JSON.stringify(options)}`);
    const newId = await papi.webViews.openWebView(webViewType, layout, newOptions);
    if (newId) {
      this.webViewIds[webViewType] = newId;
      return true;
    }
    logger.warn(`Failed to open ${webViewType} WebView for project ${this.projectId}`);
    return false;
  }

  private async getDataProvider(): Promise<
    IBaseProjectDataProvider<MandatoryProjectDataTypes> | undefined
  > {
    this.dataProvider ||= await papi.projectDataProviders.get('platform.base', this.projectId);
    return this.dataProvider;
  }
}
