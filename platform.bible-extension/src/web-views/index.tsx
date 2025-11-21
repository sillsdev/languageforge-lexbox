import type { IWebViewProvider, SavedWebViewDefinition, WebViewDefinition } from '@papi/core';
import type {
  BrowseWebViewOptions,
  DictionaryWebViewOptions,
  ProjectWebViewOptions,
} from 'fw-lite-extension';
import mainCssStyles from '../styles.css?inline';
import tailwindCssStyles from '../tailwind.css?inline';
import { WebViewType } from '../types/enums';
import fwAddWordWindow from './add-word.web-view?inline';
import fwDictionarySelectWindow from './dictionary-select.web-view?inline';
import fwFindRelatedWordsWindow from './find-related-words.web-view?inline';
import fwFindWordWindow from './find-word.web-view?inline';
import fwMainWindow from './main.web-view?inline';

const iconUrl = 'papi-extension://fw-lite-extension/assets/logo-dark.png';

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
      content: fwMainWindow,
      iconUrl,
      styles: mainCssStyles,
      title: '%fwLiteExtension_webViewTitle_browseDictionary%',
    };
  },
};

export const addWordWebViewProvider: IWebViewProvider = {
  async getWebView(
    savedWebView: SavedWebViewDefinition,
    options: DictionaryWebViewOptions,
  ): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== String(WebViewType.AddWord))
      throw new Error(
        `${WebViewType.AddWord} provider received request to provide a ${savedWebView.webViewType} WebView`,
      );
    return {
      ...savedWebView,
      ...options,
      content: fwAddWordWindow,
      iconUrl,
      styles: tailwindCssStyles,
      title: '%fwLiteExtension_webViewTitle_addWord%',
    };
  },
};

export const dictionarySelectWebViewProvider: IWebViewProvider = {
  async getWebView(
    savedWebView: SavedWebViewDefinition,
    options: ProjectWebViewOptions,
  ): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== String(WebViewType.DictionarySelect))
      throw new Error(
        `${WebViewType.DictionarySelect} provider received request to provide a ${savedWebView.webViewType} WebView`,
      );
    return {
      ...savedWebView,
      ...options,
      content: fwDictionarySelectWindow,
      iconUrl,
      styles: tailwindCssStyles,
      title: '%fwLiteExtension_webViewTitle_selectDictionary%',
    };
  },
};

export const findWordWebViewProvider: IWebViewProvider = {
  async getWebView(
    savedWebView: SavedWebViewDefinition,
    options: DictionaryWebViewOptions,
  ): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== String(WebViewType.FindWord))
      throw new Error(
        `${WebViewType.FindWord} provider received request to provide a ${savedWebView.webViewType} WebView`,
      );
    return {
      ...savedWebView,
      ...options,
      content: fwFindWordWindow,
      iconUrl,
      styles: tailwindCssStyles,
      title: '%fwLiteExtension_webViewTitle_findWord%',
    };
  },
};

export const findRelatedWordsWebViewProvider: IWebViewProvider = {
  async getWebView(
    savedWebView: SavedWebViewDefinition,
    options: DictionaryWebViewOptions,
  ): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== String(WebViewType.FindRelatedWords))
      throw new Error(
        `${WebViewType.FindRelatedWords} provider received request to provide a ${savedWebView.webViewType} WebView`,
      );
    return {
      ...savedWebView,
      ...options,
      content: fwFindRelatedWordsWindow,
      iconUrl,
      styles: tailwindCssStyles,
      title: '%fwLiteExtension_webViewTitle_findRelatedWords%',
    };
  },
};
