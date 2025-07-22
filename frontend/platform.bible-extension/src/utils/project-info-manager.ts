import papi, { logger } from '@papi/backend';
import { type ProjectInfo, ProjectSettingKey, type WebViewKey } from 'fw-lite-extension';

export class ProjectInfoManager {
  readonly projectInfo: { [projectId: string]: ProjectInfo } = {};

  static async getProjectIdFromWebViewId(webViewId: string): Promise<string | undefined> {
    const webViewDef = await papi.webViews
      .getOpenWebViewDefinition(webViewId)
      .catch((e) => void logger.error('Error getting web view definition:', e));
    if (!webViewDef?.projectId) {
      logger.warn(`No projectId found for web view '${webViewId}'`);
      return;
    }
    return webViewDef.projectId;
  }

  static async createFromProjectId(projectId: string): Promise<ProjectInfoManager | undefined> {
    if (!projectId) return;
    const infoManager = new ProjectInfoManager();
    await infoManager.getProjectInfo(projectId);
    return infoManager;
  }

  static async createFromWebViewId(webViewId: string): Promise<ProjectInfoManager | undefined> {
    const projectId = await ProjectInfoManager.getProjectIdFromWebViewId(webViewId);
    return await this.createFromProjectId(projectId ?? '');
  }

  async addWebViewId(webViewKey: WebViewKey, webViewId: string, projectId?: string): Promise<void> {
    projectId ||= (await ProjectInfoManager.getProjectIdFromWebViewId(webViewId)) ?? '';
    const info = await this.getProjectInfo(projectId);
    if (!info) return;
    info.webViewIds ??= {};
    info.webViewIds[webViewKey] = webViewId;
  }

  async getWebViewId(projectId: string, webViewKey: WebViewKey): Promise<string | undefined> {
    const info = await this.getProjectInfo(projectId);
    return info?.webViewIds?.[webViewKey];
  }

  async getProjectInfo(projectId: string): Promise<ProjectInfo | undefined> {
    if (!projectId) return;
    if (!(projectId in this.projectInfo)) {
      const dataProvider = await papi.projectDataProviders.get('platform.base', projectId);
      this.projectInfo[projectId] = { dataProvider };
    }
    return this.projectInfo[projectId];
  }

  async getLanguage(projectId: string): Promise<string | undefined> {
    const info = await this.getProjectInfo(projectId);
    if (!info?.dataProvider) return;
    if (!info.language) {
      info.language = await info.dataProvider.getSetting(ProjectSettingKey.ProjectLanguage);
    }
    return info.language;
  }

  async getName(projectId: string): Promise<string | undefined> {
    const info = await this.getProjectInfo(projectId);
    if (!info?.dataProvider) return;
    if (!info.name) {
      info.name = await info.dataProvider.getSetting(ProjectSettingKey.ProjectName);
    }
    return info.name;
  }

  async getFwDictionaryCode(projectId: string): Promise<string | undefined> {
    const info = await this.getProjectInfo(projectId);
    if (!info?.dataProvider) return;
    if (info.fwDictionaryCode === undefined) {
      info.fwDictionaryCode = await info.dataProvider.getSetting(
        ProjectSettingKey.FwDictionaryCode,
      );
    }
    return info.fwDictionaryCode;
  }

  async setFwDictionaryCode(projectId: string, dictionaryCode: string): Promise<void> {
    if ((await this.getFwDictionaryCode(projectId)) === dictionaryCode) return;
    const info = await this.getProjectInfo(projectId);
    if (!info?.dataProvider) return;
    await info.dataProvider.setSetting(ProjectSettingKey.FwDictionaryCode, dictionaryCode);
    info.fwDictionaryCode = dictionaryCode;
  }
}
