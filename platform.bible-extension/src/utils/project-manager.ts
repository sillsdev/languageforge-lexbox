import { localization, logger, notifications, projectDataProviders, webViews } from '@papi/backend';
import type { MandatoryProjectDataTypes } from '@papi/core';
import type { LexiconWebViewOptions, ProjectWebViewOptions, WebViewIds } from 'lexicon';
import type { IBaseProjectDataProvider } from 'papi-shared-types';
import { formatReplacementString, getErrorMessage } from 'platform-bible-utils';
// eslint-disable-next-line no-restricted-imports
import type { Layout } from 'shared/models/docking-framework.model';
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

  async getLexiconCodeOrOpenSelector(): Promise<string | void> {
    const lexiconCode = await this.getSetting(ProjectSettingKey.LexiconCode);
    const nameOrId = await this.getNameOrId();
    if (lexiconCode) {
      if (await this.isLexiconCodeValid(lexiconCode)) {
        logger.info(`Project '${nameOrId}' is using lexicon '${lexiconCode}'`);
        return lexiconCode;
      }
      // The stored lexicon no longer resolves (e.g. it was deleted in FW Lite). Clear it so the
      // project isn't stuck pointing at a missing lexicon (which would cause every action to open a
      // broken view), then fall through to prompt for a new selection.
      logger.warn(
        `Lexicon '${lexiconCode}' for project '${nameOrId}' no longer resolves; clearing`,
      );
      await this.setLexiconCode('');
      await ProjectManager.notifyLexiconMissing(lexiconCode);
    } else {
      logger.info(`Lexicon not yet selected for project '${nameOrId}'`);
    }

    await this.openSelector();
  }

  /**
   * Opens the lexicon selector for this project.
   *
   * One selector serves a project, so opening it again re-aims the one already open rather than
   * adding a second: the user makes one choice, and it reaches whoever asked for it last. An
   * earlier asker simply never hears back, which is the same state as a dismissed selector.
   *
   * @param resultCommand - Names a command the selector reports the chosen lexicon to instead of
   *   recording it in `lexicon.lexiconCode`, for a caller that keeps the project-to-lexicon link
   *   elsewhere. Absent for this extension's own selections, which the selector records.
   * @returns Whether the selector opened, which is not whether a lexicon was chosen.
   */
  async openSelector(resultCommand?: string): Promise<boolean> {
    const vernacularLanguage = await this.getLanguageTag();
    const options: LexiconWebViewOptions = { resultCommand, vernacularLanguage };
    return await this.openWebView(
      WebViewType.SelectLexicon,
      { floatSize: { height: 500, width: 400 }, type: 'float' },
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
