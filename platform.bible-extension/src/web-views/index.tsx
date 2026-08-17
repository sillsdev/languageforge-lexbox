import papi from '@papi/backend';
import type { IWebViewProvider, SavedWebViewDefinition, WebViewDefinition } from '@papi/core';
import type { BrowseWebViewOptions, LexiconWebViewOptions } from 'lexicon';
import { formatReplacementString } from 'platform-bible-utils';
import mainCssStyles from '../styles.css?inline';
import tailwindCssStyles from '../tailwind.css?inline';
import { WebViewType } from '../types/enums';
import addWordWindow from './add-word.web-view?inline';
import selectLexiconWindow from './select-lexicon.web-view?inline';
import findRelatedWordsWindow from './find-related-words.web-view?inline';
import findWordWindow from './find-word.web-view?inline';
import mainWindow from './main.web-view?inline';

const iconUrl = 'papi-extension://lexicon/assets/logo-dark.png';

/* eslint-disable @typescript-eslint/require-await */

export const mainWebViewProvider: IWebViewProvider = {
  async getWebView(
    savedWebView: SavedWebViewDefinition,
    options: BrowseWebViewOptions,
  ): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== String(WebViewType.Main))
      throw new Error(
        `${WebViewType.Main} provider received request to provide a ${savedWebView.webViewType} WebView`,
      );
    return {
      ...savedWebView,
      ...options,
      allowedFrameSources: ['http://localhost:*'],
      content: mainWindow,
      iconUrl,
      styles: mainCssStyles,
      title: '%lexicon_webViewTitle_browseLexicon%',
    };
  },
};

export const addWordWebViewProvider: IWebViewProvider = {
  async getWebView(
    savedWebView: SavedWebViewDefinition,
    options: LexiconWebViewOptions,
  ): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== String(WebViewType.AddWord))
      throw new Error(
        `${WebViewType.AddWord} provider received request to provide a ${savedWebView.webViewType} WebView`,
      );
    return {
      ...savedWebView,
      ...options,
      content: addWordWindow,
      iconUrl,
      styles: tailwindCssStyles,
      title: '%lexicon_webViewTitle_addWord%',
    };
  },
};

export const selectLexiconWebViewProvider: IWebViewProvider = {
  async getWebView(
    savedWebView: SavedWebViewDefinition,
    options: LexiconWebViewOptions,
  ): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== String(WebViewType.SelectLexicon))
      throw new Error(
        `${WebViewType.SelectLexicon} provider received request to provide a ${savedWebView.webViewType} WebView`,
      );
    // Name the project in the tab when known. On a fresh open, options carries it; on restore
    // (options empty) the persisted title already holds the name. Only when a tab was never opened
    // with a project does it fall back to the generic label — the panel heading still names the
    // project once an action resolves it.
    const title = options.projectName
      ? formatReplacementString(
          await papi.localization.getLocalizedString({
            localizeKey: '%lexicon_webViewTitle_selectLexiconForProject%',
          }),
          { project: options.projectName },
        )
      : (savedWebView.title ?? '%lexicon_webViewTitle_selectLexicon%');
    return {
      ...savedWebView,
      ...options,
      content: selectLexiconWindow,
      iconUrl,
      styles: tailwindCssStyles,
      title,
    };
  },
};

export const findWordWebViewProvider: IWebViewProvider = {
  async getWebView(
    savedWebView: SavedWebViewDefinition,
    options: LexiconWebViewOptions,
  ): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== String(WebViewType.FindWord))
      throw new Error(
        `${WebViewType.FindWord} provider received request to provide a ${savedWebView.webViewType} WebView`,
      );
    return {
      ...savedWebView,
      ...options,
      content: findWordWindow,
      iconUrl,
      styles: tailwindCssStyles,
      title: '%lexicon_webViewTitle_findWord%',
    };
  },
};

export const findRelatedWordsWebViewProvider: IWebViewProvider = {
  async getWebView(
    savedWebView: SavedWebViewDefinition,
    options: LexiconWebViewOptions,
  ): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== String(WebViewType.FindRelatedWords))
      throw new Error(
        `${WebViewType.FindRelatedWords} provider received request to provide a ${savedWebView.webViewType} WebView`,
      );
    return {
      ...savedWebView,
      ...options,
      content: findRelatedWordsWindow,
      iconUrl,
      styles: tailwindCssStyles,
      title: '%lexicon_webViewTitle_findRelatedWords%',
    };
  },
};
