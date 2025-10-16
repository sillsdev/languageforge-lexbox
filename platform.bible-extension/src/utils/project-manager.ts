import { logger, projectDataProviders, webViews } from '@papi/backend';
import type { MandatoryProjectDataTypes } from '@papi/core';
import type {
  DictionaryWebViewOptions,
  ProjectWebViewOptions,
  WebViewIds,
} from 'fw-lite-extension';
import type { IBaseProjectDataProvider } from 'papi-shared-types';
// eslint-disable-next-line no-restricted-imports
import type { Layout } from 'shared/models/docking-framework.model';
import { ProjectSettingKey, WebViewType } from '../types/enums';

export class ProjectManager {
  readonly projectId: string;
  private dataProvider?: IBaseProjectDataProvider<MandatoryProjectDataTypes>;
  private readonly webViewIds: WebViewIds = {};

  constructor(projectId: string) {
    this.projectId = projectId;
  }

  static async getFwDictionaryCode(projectId: string): Promise<string | undefined> {
    return await new ProjectManager(projectId).getFwDictionaryCode();
  }

  async getFwAnalysisLanguage(): Promise<string | undefined> {
    return await this.getSetting(ProjectSettingKey.FwAnalysisLanguage);
  }

  async setFwAnalysisLanguage(analysisLanguage: string): Promise<void> {
    if ((await this.getFwAnalysisLanguage()) === analysisLanguage) return;
    await this.setSetting(ProjectSettingKey.FwAnalysisLanguage, analysisLanguage);
  }

  async getFwDictionaryCode(): Promise<string | undefined> {
    return await this.getSetting(ProjectSettingKey.FwDictionaryCode);
  }

  async getFwDictionaryCodeOrOpenSelector(): Promise<string | void> {
    const dictionaryCode = await this.getSetting(ProjectSettingKey.FwDictionaryCode);
    const nameOrId = await this.getNameOrId();
    if (dictionaryCode) {
      logger.info(`Project '${nameOrId}' is using FieldWorks dictionary '${dictionaryCode}'`);
      return dictionaryCode;
    }

    logger.info(`FieldWorks dictionary not yet selected for project '${nameOrId}'`);
    await this.openWebView(WebViewType.DictionarySelect, {
      floatSize: { height: 500, width: 400 },
      type: 'float',
    });
  }

  async setFwDictionaryCode(dictionaryCode: string): Promise<void> {
    if ((await this.getFwDictionaryCode()) === dictionaryCode) return;
    await this.setSetting(ProjectSettingKey.FwDictionaryCode, dictionaryCode);
  }

  async getLanguageTag(): Promise<string | undefined> {
    return await this.getSetting(ProjectSettingKey.ProjectLanguageTag);
  }

  async getName(): Promise<string | undefined> {
    return await this.getSetting(ProjectSettingKey.ProjectName);
  }

  async getNameOrId(): Promise<string | undefined> {
    return (await this.getName()) || this.projectId;
  }

  async getDictionaryWebViewOptions(word?: string): Promise<DictionaryWebViewOptions> {
    return {
      analysisLanguage: await this.getFwAnalysisLanguage(),
      dictionaryCode: await this.getFwDictionaryCode(),
      vernacularLanguage: await this.getLanguageTag(),
      word,
    };
  }

  async openWebView(
    webViewType: WebViewType,
    layout?: Layout,
    options?: ProjectWebViewOptions,
  ): Promise<boolean> {
    const webViewId = this.webViewIds[webViewType];
    const newOptions = { ...options, projectId: this.projectId };
    logger.info(`Opening ${webViewType} WebView for project ${this.projectId}`);
    logger.info(`WebView options: ${JSON.stringify(newOptions)}`);
    if (webViewId && (await webViews.reloadWebView(webViewType, webViewId, newOptions))) {
      return true;
    }
    this.webViewIds[webViewType] = await webViews.openWebView(webViewType, layout, newOptions);
    if (this.webViewIds[webViewType]) {
      return true;
    }
    logger.warn(`Failed to open ${webViewType} WebView for project ${this.projectId}`);
    return false;
  }

  private async getDataProvider(): Promise<
    IBaseProjectDataProvider<MandatoryProjectDataTypes> | undefined
  > {
    this.dataProvider ||= await projectDataProviders.get('platform.base', this.projectId);
    return this.dataProvider;
  }

  private async getSetting(setting: ProjectSettingKey): Promise<string | undefined> {
    logger.info(`Getting '${ProjectSettingKey.ProjectLanguageTag}'`);
    return await (await this.getDataProvider())?.getSetting(setting);
  }

  private async setSetting(setting: ProjectSettingKey, value: string): Promise<void> {
    logger.info(`Setting '${ProjectSettingKey.ProjectLanguageTag}' to '${value}'`);
    await (await this.getDataProvider())?.setSetting(setting, value);
  }
}
