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

  static async getLexiconCode(projectId: string): Promise<string | undefined> {
    return await new ProjectManager(projectId).getLexiconCode();
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

  async openSelector(): Promise<boolean> {
    const vernacularLanguage = await this.getLanguageTag();
    // Current lexicon (if any) so the selector can pre-select it.
    const lexiconCode = await this.getLexiconCode();
    const projectName = await this.getName();
    const options: LexiconWebViewOptions = { vernacularLanguage, lexiconCode, projectName };
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
