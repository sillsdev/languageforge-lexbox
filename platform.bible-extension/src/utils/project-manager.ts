import { localization, logger, notifications, projectDataProviders, webViews } from '@papi/backend';
import type { MandatoryProjectDataTypes } from '@papi/core';
import type { LexiconWebViewOptions, ProjectWebViewOptions, WebViewIds } from 'lexicon';
import type { IBaseProjectDataProvider } from 'papi-shared-types';
import { formatReplacementString, getErrorMessage } from 'platform-bible-utils';
// eslint-disable-next-line no-restricted-imports
import type { Layout } from 'shared/models/docking-framework.model';
import type { LexiconPickerProjectInfo } from './fw-lite-api';
import { ProjectSettingKey, WebViewType } from '../types/enums';

export class ProjectManager {
  readonly projectId: string;
  private dataProvider?: IBaseProjectDataProvider<MandatoryProjectDataTypes>;
  private readonly webViewIds: WebViewIds = {};
  private readonly isLexiconCodeValid: (lexiconCode: string) => Promise<boolean>;

  constructor(projectId: string, isLexiconCodeValid?: (lexiconCode: string) => Promise<boolean>) {
    this.projectId = projectId;
    this.isLexiconCodeValid = isLexiconCodeValid ?? (async () => true);
  }

  /** Tells the user why their lexicon selection was discarded, so the selector isn't unexplained. */
  private static async notifyLexiconMissing(lexiconCode: string): Promise<void> {
    try {
      const template = await localization.getLocalizedString({
        localizeKey: '%lexicon_error_lexiconMissing%',
      });
      await notifications.send({
        message: formatReplacementString(template, { lexiconCode }),
        severity: 'error',
      });
    } catch (e) {
      // A failed notification must not stop the selector from opening.
      logger.warn('Could not notify user of the missing lexicon:', getErrorMessage(e));
    }
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

  /** Returns the stored lexicon code if it still resolves; a stale one is cleared. */
  async getValidLexiconCode(): Promise<string | void> {
    const lexiconCode = await this.getSetting(ProjectSettingKey.LexiconCode);
    const nameOrId = await this.getNameOrId();
    if (!lexiconCode) {
      logger.info(`Lexicon not yet selected for project '${nameOrId}'`);
      return;
    }
    if (await this.isLexiconCodeValid(lexiconCode)) {
      logger.info(`Project '${nameOrId}' is using lexicon '${lexiconCode}'`);
      return lexiconCode;
    }
    // The stored lexicon no longer resolves (e.g. deleted in FW Lite). Clear it — otherwise every
    // action opens a broken view — so the caller can prompt for a new selection.
    logger.warn(`Lexicon '${lexiconCode}' for project '${nameOrId}' no longer resolves; clearing`);
    await this.setLexiconCode('');
    await ProjectManager.notifyLexiconMissing(lexiconCode);
  }

  async getLexiconCodeOrOpenSelector(): Promise<string | void> {
    const lexiconCode = await this.getValidLexiconCode();
    if (lexiconCode) return lexiconCode;
    await this.openSelector();
  }

  /**
   * Opens the lexicon selector for this project.
   *
   * One selector serves a project, so opening it again re-aims the one already open rather than
   * adding a second.
   *
   * @returns Whether the selector opened, which is not whether a lexicon was chosen.
   */
  async openSelector(): Promise<boolean> {
    // Only for the tab title. The panel reads everything else through lexicon.lexicons, since props
    // are frozen at open time and go stale on a layout restore.
    const options: LexiconWebViewOptions = { projectName: await this.getName() };
    return await this.openWebView(
      WebViewType.SelectLexicon,
      // Tall enough for the account section plus a useful slice of the list (see LexiconPicker).
      { floatSize: { height: 640, width: 440 }, type: 'float' },
      options,
    );
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

  /** A failed read leaves that field unset instead of failing the whole picker. */
  async getLexiconPickerInfo(): Promise<LexiconPickerProjectInfo> {
    const [name, lexiconCode, langTag] = await Promise.all([
      this.getSettingOrUndefined(ProjectSettingKey.ProjectName),
      this.getSettingOrUndefined(ProjectSettingKey.LexiconCode),
      this.getSettingOrUndefined(ProjectSettingKey.ProjectLanguageTag),
    ]);
    return { id: this.projectId, name, lexiconCode, langTag };
  }

  /**
   * Options for a WebView scoped to one lexicon.
   *
   * These are a snapshot: an open view keeps the values it was given until a command reopens it,
   * even after the project's lexicon selection changes.
   */
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

  private async getSettingOrUndefined(setting: ProjectSettingKey): Promise<string | undefined> {
    try {
      return await this.getSetting(setting);
    } catch (e) {
      logger.warn(
        `Could not get '${setting}' for project '${this.projectId}':`,
        getErrorMessage(e),
      );
      return undefined;
    }
  }

  private async setSetting(setting: ProjectSettingKey, value: string): Promise<void> {
    logger.info(`Setting '${setting}' to '${value}'`);
    await (await this.getDataProvider())?.setSetting(setting, value);
  }
}
