import papi, { logger } from '@papi/backend';
import { ProjectManager } from './project-manager';

export class ProjectManagers {
  private readonly projectManagers: { [projectId: string]: ProjectManager } = {};

  static async getProjectIdFromWebViewId(webViewId: string): Promise<string | undefined> {
    if (!webViewId) return;
    const webViewDef = await papi.webViews
      .getOpenWebViewDefinition(webViewId)
      .catch((e) => logger.error('Error getting WebView definition:', JSON.stringify(e)));
    if (!webViewDef?.projectId) {
      logger.warn(`No projectId found for WebView '${webViewId}'`);
      return;
    }
    return webViewDef.projectId;
  }

  getProjectManagerFromProjectId(projectId: string): ProjectManager | undefined {
    if (!projectId) return;
    if (!(projectId in this.projectManagers)) {
      this.projectManagers[projectId] = new ProjectManager(projectId);
    }
    return this.projectManagers[projectId];
  }

  async getProjectManagerFromWebViewId(webViewId: string): Promise<ProjectManager | undefined> {
    const projectId = await ProjectManagers.getProjectIdFromWebViewId(webViewId);
    if (!projectId) return;
    return this.getProjectManagerFromProjectId(projectId);
  }
}
