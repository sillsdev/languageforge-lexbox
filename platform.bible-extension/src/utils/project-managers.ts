import papi, { logger } from '@papi/backend';
import { getErrorMessage } from 'platform-bible-utils';
import { ProjectManager } from './project-manager';

export class ProjectManagers {
  private readonly projectManagers: { [projectId: string]: ProjectManager } = {};
  private readonly isLexiconCodeValid: (lexiconCode: string) => Promise<boolean>;

  constructor(isLexiconCodeValid: (lexiconCode: string) => Promise<boolean>) {
    this.isLexiconCodeValid = isLexiconCodeValid;
  }

  static async getProjectIdFromWebViewId(webViewId: string): Promise<string | undefined> {
    if (!webViewId) return;
    const webViewDef = await papi.webViews
      .getOpenWebViewDefinition(webViewId)
      .catch((e) => logger.error('Error getting WebView definition:', JSON.stringify(e)));
    if (!webViewDef?.projectId) {
      logger.debug(`No projectId found for WebView '${webViewId}'`);
      return;
    }
    // A restored layout can reference a project that no longer exists; treat it as "no project"
    // (callers then prompt).
    if (!(await ProjectManagers.projectExists(webViewDef.projectId))) {
      logger.warn(
        `Project '${webViewDef.projectId}' for WebView '${webViewId}' no longer resolves; ignoring it`,
      );
      return;
    }
    return webViewDef.projectId;
  }

  // A deleted project's id makes every project-settings call fail.
  private static async projectExists(projectId: string): Promise<boolean> {
    return await papi.projectLookup
      .getMetadataForProject(projectId)
      .then(() => true)
      .catch((e) => {
        logger.warn(`Metadata lookup for project '${projectId}' failed:`, getErrorMessage(e));
        return false;
      });
  }

  async getProjectManagerFromProjectId(projectId: string): Promise<ProjectManager | undefined> {
    if (!projectId) return;
    if (!(await ProjectManagers.projectExists(projectId))) {
      logger.warn(`Project '${projectId}' no longer resolves; ignoring it`);
      return;
    }
    if (!(projectId in this.projectManagers)) {
      this.projectManagers[projectId] = new ProjectManager(projectId, this.isLexiconCodeValid);
    }
    return this.projectManagers[projectId];
  }

  /**
   * Resolves the project manager for the project a WebView is scoped to; when it has none (e.g. the
   * Scripture Editor with no project selected) the user is prompted with the core project selector.
   * Returns `undefined` only if the user dismisses without choosing.
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
    return await this.getProjectManagerFromProjectId(projectId);
  }
}
