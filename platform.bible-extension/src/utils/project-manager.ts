import { logger, projectDataProviders, webViews } from '@papi/backend';
import type { MandatoryProjectDataTypes } from '@papi/core';
import type { LexiconWebViewOptions, ProjectWebViewOptions, WebViewIds } from 'lexicon';
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

  static async getLexiconCode(projectId: string): Promise<string | undefined> {
    return await new ProjectManager(projectId).getLexiconCode();
  }

  async getAnalysisLanguage(): Promise<string | undefined> {
    return await this.getSetting(ProjectSettingKey.AnalysisLanguage);
  }

  async setAnalysisLanguage(analysisLanguage: string): Promise<void> {
    if ((await this.getAnalysisLanguage()) === analysisLanguage) return;
    await this.setSetting(ProjectSettingKey.AnalysisLanguage, analysisLanguage);
  }

  async getLexiconCode(): Promise<string | undefined> {
    return await this.getSetting(ProjectSettingKey.LexiconCode);
  }

  async getLexiconCodeOrOpenSelector(): Promise<string | void> {
    const lexiconCode = await this.getSetting(ProjectSettingKey.LexiconCode);
    const nameOrId = await this.getNameOrId();
    if (lexiconCode) {
      logger.info(`Project '${nameOrId}' is using lexicon '${lexiconCode}'`);
      return lexiconCode;
    }

    logger.info(`Lexicon not yet selected for project '${nameOrId}'`);
    await this.openWebView(WebViewType.SelectLexicon, {
      floatSize: { height: 500, width: 400 },
      type: 'float',
    });
  }

  async setLexiconCode(lexiconCode: string): Promise<void> {
    if ((await this.getLexiconCode()) === lexiconCode) return;
    await this.setSetting(ProjectSettingKey.LexiconCode, lexiconCode);
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

  async getLexiconWebViewOptions(word?: string): Promise<LexiconWebViewOptions> {
    return {
      analysisLanguage: await this.getAnalysisLanguage(),
      lexiconCode: await this.getLexiconCode(),
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
    logger.info(`Getting '${setting}'`);
    return await (await this.getDataProvider())?.getSetting(setting);
  }

  private async setSetting(setting: ProjectSettingKey, value: string): Promise<void> {
    logger.info(`Setting '${setting}' to '${value}'`);
    await (await this.getDataProvider())?.setSetting(setting, value);
  }
}
