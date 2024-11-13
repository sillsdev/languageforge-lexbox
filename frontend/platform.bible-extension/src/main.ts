import papi, {logger} from '@papi/backend';
import {
  type ExecutionActivationContext,
  type IWebViewProvider,
  type SavedWebViewDefinition,
  type WebViewDefinition,
} from '@papi/core';
import type {FindEntryEvent, LaunchServerEvent} from 'fw-lite-extension';
import extensionTemplateReact from './extension-template.web-view?inline';
import extensionTemplatestyles from './styles.css?inline';
import type {GetWebViewOptions} from 'shared/models/web-view.model';

logger.info('FwLite extension is importing!');

const reactWebViewType = 'fw-lite-extension.react';

/**
 * Simple web view provider that provides React web views when papi requests them
 */
const reactWebViewProvider: IWebViewProvider = {
  async getWebView(savedWebView: SavedWebViewDefinition, options: GetWebViewOptions): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== reactWebViewType)
      throw new Error(
        `${reactWebViewType} provider received request to provide a ${savedWebView.webViewType} web view`,
      );
    return {
      ...savedWebView,
      title: 'FW Lite Extension React',
      content: extensionTemplateReact,
      styles: extensionTemplatestyles,
      iconUrl: 'papi-extension://fw-lite-extension/assets/logo-dark.png'
    };
  },
};

export async function activate(context: ExecutionActivationContext) {
  logger.info('FwLite is activating!');

  const reactWebViewProviderPromise = papi.webViewProviders.register(
    reactWebViewType,
    reactWebViewProvider,
  );

  // Emitter to tell subscribers how many times we have done stuff
  const onFindEntryEmitter = papi.network.createNetworkEventEmitter<FindEntryEvent>(
    'fwLiteExtension.findEntry',
  );
  const onLaunchServerEmitter = papi.network.createNetworkEventEmitter<LaunchServerEvent>(
    'fwLiteExtension.launchServer',
  );
  let baseUrlHolder = {baseUrl: ''};
  launchFwLiteLocalWebApp(context).then(baseUrl => {
    baseUrlHolder.baseUrl = baseUrl;
    onLaunchServerEmitter.emit({baseUrl});
  });

  const getBaseUrlCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.getBaseUrl',
    () => {
      return baseUrlHolder;
    },
  );

  const findEntryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.findEntry',
    (entry: string) => {
      onFindEntryEmitter.emit({entry});
      return {
        success: true,
      };
    },
  );
  const simpleFindEntryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.simpleFind',
    () => {
      onFindEntryEmitter.emit({entry: 'apple'});
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
  void papi.webViews.getWebView(reactWebViewType, undefined, {existingId: '?'});

  // Await the data provider promise at the end so we don't hold everything else up
  context.registrations.add(
    await reactWebViewProviderPromise,
    await findEntryCommandPromise,
    await simpleFindEntryCommandPromise,
    await getBaseUrlCommandPromise,
    onFindEntryEmitter,
    onLaunchServerEmitter
  );

  logger.info('FwLite is finished activating!');
}

export async function deactivate() {
  logger.info('FwLite is deactivating!');
  return true;
}

async function launchFwLiteLocalWebApp(context: ExecutionActivationContext) {
  let binaryPath = 'fw-lite/LocalWebApp.exe';
  if (context.elevatedPrivileges.createProcess === undefined) {
    throw new Error('FwLite requires createProcess elevated privileges');
  }
  if (context.elevatedPrivileges.createProcess.osData.platform !== 'win32') {
    throw new Error('FwLite only supports launching on windows for now');
  }
  //todo instead of hardcoding the url and port we should run it and find the url in the output
  let baseUrl = 'http://localhost:29348';
  context.elevatedPrivileges.createProcess.spawn(
    context.executionToken,
    binaryPath,
    [
      '--urls', baseUrl,
      '--LocalWebApp:OpenBrowser=false',
      '--LocalWebApp:CorsAllowAny=true',
      '--LocalWebApp:LogFileName=fw-lite-local-web-app.log',
    ],
    {stdio: [null, null, null]}
  );
  return baseUrl;
}
