import papi, { logger } from '@papi/backend';
import {
  type ExecutionActivationContext,
  type IWebViewProvider,
  type OpenWebViewOptions,
  type SavedWebViewDefinition,
  type WebViewDefinition,
} from '@papi/core';
import type {
  FindEntryEvent,
  FwProject,
  LaunchServerEvent,
  LocalProjectsEvent,
} from 'fw-lite-extension';
import extensionTemplateReact from './extension-template.web-view?inline';
import extensionTemplateStyles from './styles.css?inline';

const reactWebViewType = 'fw-lite-extension.react';

/**
 * Simple web view provider that provides React web views when papi requests them
 */
const reactWebViewProvider: IWebViewProvider = {
  // eslint-disable-next-line @typescript-eslint/require-await
  async getWebView(
    savedWebView: SavedWebViewDefinition,
    _: OpenWebViewOptions,
  ): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== reactWebViewType)
      throw new Error(
        `${reactWebViewType} provider received request to provide a ${savedWebView.webViewType} web view`,
      );
    return {
      ...savedWebView,
      title: 'FieldWorks Lite Extension React',
      content: extensionTemplateReact,
      styles: extensionTemplateStyles,
      iconUrl: 'papi-extension://fw-lite-extension/assets/logo-dark.png',
      allowedFrameSources: ['http://localhost:*'],
    };
  },
};

export async function activate(context: ExecutionActivationContext): Promise<void> {
  logger.info('FieldWorks Lite is activating!');

  const reactWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
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
  const onLocalProjectsEmitter = papi.network.createNetworkEventEmitter<LocalProjectsEvent>(
    'fwLiteExtension.localProjects',
  );

  const baseUrlHolder = { baseUrl: '' };
  const { fwLiteProcess, baseUrl } = launchFwLiteFwLiteWeb(context);
  baseUrlHolder.baseUrl = baseUrl;
  onLaunchServerEmitter.emit(baseUrlHolder);

  const getBaseUrlCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.getBaseUrl',
    () => {
      return baseUrlHolder;
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
  const openFwLiteCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.openFWLite',
    () => {
      void papi.webViews.openWebView(reactWebViewType, undefined, { existingId: '?' });
      return { success: true };
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

  const localProjectsCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.localProjects',
    async () => {
      logger.info('Fetching local FieldWorks projects');
      const response = await papi.fetch(`${baseUrl}${'/api/localProjects'}`);
      const jsonText = await (await response.blob()).text();
      const projects = JSON.parse(jsonText) as FwProject[];
      onLocalProjectsEmitter.emit({ projects });
    },
  );

  // Create WebViews or get an existing WebView if one already exists for this type
  // Note: here, we are using `existingId: '?'` to indicate we do not want to create a new WebView
  // if one already exists. The WebView that already exists could have been created by anyone
  // anywhere; it just has to match `webViewType`. See `paranext-core's hello-someone.ts` for an
  // example of keeping an existing WebView that was specifically created by
  // `paranext-core's hello-someone`.
  void papi.webViews.openWebView(reactWebViewType, undefined, { existingId: '?' });

  // Await the data provider promise at the end so we don't hold everything else up
  context.registrations.add(
    await reactWebViewProviderPromise,
    await findEntryCommandPromise,
    await simpleFindEntryCommandPromise,
    await getBaseUrlCommandPromise,
    await openFwLiteCommandPromise,
    await localProjectsCommandPromise,
    onFindEntryEmitter,
    onLaunchServerEmitter,
    () => fwLiteProcess?.kill(),
  );

  logger.info('FieldWorks Lite is finished activating!');
}

// eslint-disable-next-line @typescript-eslint/require-await
export async function deactivate(): Promise<boolean> {
  logger.info('FieldWorks Lite is deactivating!');
  return true;
}

// ts gets picky because this function can throw errors. Ignoring this because we aren't catching them at the moment
// eslint-disable-next-line  @typescript-eslint/explicit-function-return-type
function launchFwLiteFwLiteWeb(context: ExecutionActivationContext) {
  const binaryPath = 'fw-lite/FwLiteWeb.exe';
  if (context.elevatedPrivileges.createProcess === undefined) {
    throw new Error('FieldWorks Lite requires createProcess elevated privileges');
  }
  if (context.elevatedPrivileges.createProcess.osData.platform !== 'win32') {
    throw new Error('FieldWorks Lite only supports launching on Windows for now');
  }
  //todo instead of hardcoding the url and port we should run it and find the url in the output
  const baseUrl = 'http://localhost:29348';

  const fwLiteProcess = context.elevatedPrivileges.createProcess.spawn(
    context.executionToken,
    binaryPath,
    [
      '--urls',
      baseUrl,
      '--FwLiteWeb:OpenBrowser=false',
      '--FwLiteWeb:CorsAllowAny=true',
      '--FwLiteWeb:LogFileName=./../../fw-lite-web.log', //required at dev time otherwise the log file will trigger a restart of the extension by PT
    ],
    { stdio: [null, null, null] },
  );
  return { baseUrl, fwLiteProcess };
}
