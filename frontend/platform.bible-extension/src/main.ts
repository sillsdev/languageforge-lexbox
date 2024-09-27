import papi, { logger } from '@papi/backend';
import {
  type ExecutionActivationContext,
  type IWebViewProvider,
  type SavedWebViewDefinition,
  type WebViewDefinition,
} from '@papi/core';
import type {
  DoStuffEvent, FindEntryEvent,
} from 'fw-lite-extension';
import extensionTemplateReact from './extension-template.web-view?inline';

// eslint-disable-next-line
console.log(process.env.NODE_ENV);

logger.info('Extension template is importing!');

const reactWebViewType = 'paranextExtensionTemplate.react';

/**
 * Simple web view provider that provides React web views when papi requests them
 */
const reactWebViewProvider: IWebViewProvider = {
  async getWebView(savedWebView: SavedWebViewDefinition): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== reactWebViewType)
      throw new Error(
        `${reactWebViewType} provider received request to provide a ${savedWebView.webViewType} web view`,
      );
    return {
      ...savedWebView,
      title: 'FW Lite Extension React',
      content: extensionTemplateReact
    };
  },
};

export async function activate(context: ExecutionActivationContext) {
  logger.info('Extension template is activating!');

  const reactWebViewProviderPromise = papi.webViewProviders.register(
    reactWebViewType,
    reactWebViewProvider,
  );

  // Emitter to tell subscribers how many times we have done stuff
  const onDoStuffEmitter = papi.network.createNetworkEventEmitter<DoStuffEvent>(
    'fwLiteExtension.doStuff',
  );
  const onFindEntryEmitter = papi.network.createNetworkEventEmitter<FindEntryEvent>(
    'fwLiteExtension.findEntry',
  );

  let doStuffCount = 0;
  const doStuffCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.doStuff',
    (message: string) => {
      doStuffCount += 1;
      // Inform subscribers of the update
      onDoStuffEmitter.emit({ count: doStuffCount });

      // Respond to the sender of the command with the news
      return {
        response: `The template did stuff ${doStuffCount} times! ${message}`,
        occurrence: doStuffCount,
      };
    },
  );
  const findEntryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.findEntry',
    (entry: string) => {
      onFindEntryEmitter.emit({ entry });
      return {
        success: true,
      };
    },
  );
  const simpleFindEntryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.simpleFind',
    () => {
      onFindEntryEmitter.emit({ entry: 'apple' });
      return {
        success: true,
      };
    },
  );

  // Create WebViews or get an existing WebView if one already exists for this type
  // Note: here, we are using `existingId: '?'` to indicate we do not want to create a new WebView
  // if one already exists. The WebView that already exists could have been created by anyone
  // anywhere; it just has to match `webViewType`. See `paranext-core's hello-someone.ts` for an
  // example of keeping an existing WebView that was specifically created by
  // `paranext-core's hello-someone`.
  papi.webViews.getWebView(reactWebViewType, undefined, { existingId: '?' });

  // Await the data provider promise at the end so we don't hold everything else up
  context.registrations.add(
    await reactWebViewProviderPromise,
    onDoStuffEmitter,
    await doStuffCommandPromise,
    await findEntryCommandPromise,
    await simpleFindEntryCommandPromise
  );

  logger.info('Extension template is finished activating!');
}

export async function deactivate() {
  logger.info('Extension template is deactivating!');
  return true;
}
