import { logger, projectDataProviders, webViews } from '@papi/backend';
import type { MandatoryProjectDataTypes } from '@papi/core';
import type {
  DictionaryWebViewOptions,
  ProjectWebViewOptions,
  WebViewIds,
} from 'dictionary';
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

  static async getDictionaryCode(projectId: string): Promise<string | undefined> {
    return await new ProjectManager(projectId).getDictionaryCode();
  }

  async getAnalysisLanguage(): Promise<string | undefined> {
    return await this.getSetting(ProjectSettingKey.AnalysisLanguage);
  }

  async setAnalysisLanguage(analysisLanguage: string): Promise<void> {
    if ((await this.getAnalysisLanguage()) === analysisLanguage) return;
    await this.setSetting(ProjectSettingKey.AnalysisLanguage, analysisLanguage);
  }

  async getDictionaryCode(): Promise<string | undefined> {
    return await this.getSetting(ProjectSettingKey.DictionaryCode);
  }

  async getDictionaryCodeOrOpenSelector(): Promise<string | void> {
    const dictionaryCode = await this.getSetting(ProjectSettingKey.DictionaryCode);
    const nameOrId = await this.getNameOrId();
    if (dictionaryCode) {
      logger.info(`Project '${nameOrId}' is using dictionary '${dictionaryCode}'`);
      return dictionaryCode;
    }

    logger.info(`Dictionary not yet selected for project '${nameOrId}'`);
    await this.openWebView(WebViewType.DictionarySelect, {
      floatSize: { height: 500, width: 400 },
      type: 'float',
    });
  }

  async setDictionaryCode(dictionaryCode: string): Promise<void> {
    if ((await this.getDictionaryCode()) === dictionaryCode) return;
    await this.setSetting(ProjectSettingKey.DictionaryCode, dictionaryCode);
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
      analysisLanguage: await this.getAnalysisLanguage(),
      dictionaryCode: await this.getDictionaryCode(),
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
