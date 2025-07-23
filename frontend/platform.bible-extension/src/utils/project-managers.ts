import papi, { logger } from '@papi/backend';
import type { MandatoryProjectDataTypes } from '@papi/core';
import type { OpenWebViewOptionsWithProjectId, WebViewIds, WebViewType } from 'fw-lite-extension';
import type { IBaseProjectDataProvider } from 'papi-shared-types';
import type { Layout } from 'shared/models/docking-framework.model';
import { ProjectSettingKey } from '../types/enums';

export class ProjectManager {
  private dataProvider?: IBaseProjectDataProvider<MandatoryProjectDataTypes>;
  private fwDictionaryCode?: string;
  private language?: string;
  private name?: string;
  readonly projectId: string;
  private readonly webViewIds: WebViewIds = {};

  constructor(projectId: string) {
    this.projectId = projectId;
  }

  static async getFwDictionaryCode(projectId: string): Promise<string | undefined> {
    return await new ProjectManager(projectId).getFwDictionaryCode();
  }

  async getDataProvider(): Promise<
    IBaseProjectDataProvider<MandatoryProjectDataTypes> | undefined
  > {
    this.dataProvider ||= await papi.projectDataProviders.get('platform.base', this.projectId);
    return this.dataProvider;
  }

  async getFwDictionaryCode(): Promise<string | undefined> {
    this.fwDictionaryCode ??= await (
      await this.getDataProvider()
    )?.getSetting(ProjectSettingKey.FwDictionaryCode);
    return this.fwDictionaryCode;
  }

  async setFwDictionaryCode(dictionaryCode: string): Promise<void> {
    if ((await this.getFwDictionaryCode()) === dictionaryCode) return;
    const dataProvider = await this.getDataProvider();
    if (dataProvider?.setSetting(ProjectSettingKey.FwDictionaryCode, dictionaryCode)) {
      this.fwDictionaryCode = dictionaryCode;
    }
  }

  async getLanguage(): Promise<string | undefined> {
    this.language ??= await (
      await this.getDataProvider()
    )?.getSetting(ProjectSettingKey.ProjectLanguage);
    return this.language;
  }

  async getName(): Promise<string | undefined> {
    this.name ??= await (await this.getDataProvider())?.getSetting(ProjectSettingKey.ProjectName);
    return this.name;
  }

  async openWebView(
    webViewType: WebViewType,
    layout?: Layout,
    options?: OpenWebViewOptionsWithProjectId,
  ): Promise<boolean> {
    options = { ...options, existingId: this.webViewIds[webViewType], projectId: this.projectId };
    const newId = await papi.webViews.openWebView(webViewType, layout, options);
    if (newId) {
      this.webViewIds[webViewType] = newId;
      return true;
    }
    return false;
  }
}

export class ProjectManagers {
  private readonly projectInfo: { [projectId: string]: ProjectManager } = {};

  static async getProjectIdFromWebViewId(webViewId: string): Promise<string | undefined> {
    if (!webViewId) return;
    const webViewDef = await papi.webViews
      .getOpenWebViewDefinition(webViewId)
      .catch((e) => void logger.error('Error getting web view definition:', e));
    if (!webViewDef?.projectId) {
      logger.warn(`No projectId found for web view '${webViewId}'`);
      return;
    }
    return webViewDef.projectId;
  }

  getProjectManagerFromProjectId(projectId: string): ProjectManager | undefined {
    if (!projectId) return;
    if (!(projectId in this.projectInfo)) {
      this.projectInfo[projectId] = new ProjectManager(projectId);
    }
    return this.projectInfo[projectId];
  }

  async getProjectManagerFromWebViewId(webViewId: string): Promise<ProjectManager | undefined> {
    const projectId = (await ProjectManagers.getProjectIdFromWebViewId(webViewId)) ?? '';
    return this.getProjectManagerFromProjectId(projectId);
  }
}
