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
      logger.debug(`No projectId found for WebView '${webViewId}'`);
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

  /**
   * Resolves the project manager for the project a WebView is scoped to. When the WebView has no
   * associated project — e.g. the Scripture Editor is open with no project selected — the user is
   * prompted with the core project selector and their choice is used instead. Returns `undefined`
   * only when the user dismisses the selector without choosing a project.
   */
  async getProjectManagerFromWebViewIdOrSelectProject(
    webViewId: string,
  ): Promise<ProjectManager | undefined> {
    const projectId =
      (await ProjectManagers.getProjectIdFromWebViewId(webViewId)) ??
      (await papi.dialogs.selectProject({
        prompt: '%lexicon_selectProject_prompt%',
        title: '%lexicon_selectProject_title%',
      }));
    if (!projectId) return;
    return this.getProjectManagerFromProjectId(projectId);
  }
}
