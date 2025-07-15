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
  IProjectModel,
  LaunchServerEvent,
  LocalProjectsEvent,
  OpenProjectEvent,
} from 'fw-lite-extension';
import fwLiteProjectSelect from './fw-lite-project-select.web-view?inline';
import fwLiteMainWindow from './fwLiteMainWindow.web-view?inline';
import extensionTemplateStyles from './styles.css?inline';
import { EntryService } from './entry-service';

const reactWebViewType = 'fw-lite-extension.react';
const projectSelectWebViewType = 'fw-lite-project-select.react';

/**
 * Simple web view provider that provides React web views when papi requests them
 */
const reactWebViewProvider: IWebViewProvider = {
  async getWebView(
    savedWebView: SavedWebViewDefinition,
    options: OpenWebViewOptions & { projectId?: string },
  ): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== reactWebViewType)
      throw new Error(
        `${reactWebViewType} provider received request to provide a ${savedWebView.webViewType} web view`,
      );
    return {
      ...savedWebView,
      title: 'FieldWorks Lite Extension React',
      content: fwLiteMainWindow,
      styles: extensionTemplateStyles,
      iconUrl: 'papi-extension://fw-lite-extension/assets/logo-dark.png',
      allowedFrameSources: ['http://localhost:*'],
    };
  },
};

const projectSelectWebViewProvider: IWebViewProvider = {
  async getWebView(
    savedWebView: SavedWebViewDefinition,
    options: OpenWebViewOptions & { projectId?: string },
  ): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== projectSelectWebViewType)
      throw new Error(
        `${projectSelectWebViewType} provider received request to provide a ${savedWebView.webViewType} web view`,
      );
    return {
      ...savedWebView,
      title: 'Select a FieldWorks project',
      content: fwLiteProjectSelect,
      iconUrl: 'papi-extension://fw-lite-extension/assets/logo-dark.png',
    };
  },
};

export async function activate(context: ExecutionActivationContext): Promise<void> {
  logger.info('FieldWorks Lite is activating!');

  const reactWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    reactWebViewType,
    reactWebViewProvider,
  );

  const projectSelectWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    projectSelectWebViewType,
    projectSelectWebViewProvider,
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
  const onOpenProjectEmitter = papi.network.createNetworkEventEmitter<OpenProjectEvent>(
    'fwLiteExtension.openProject',
  );

  const baseUrlHolder = { baseUrl: '' };
  const { fwLiteProcess, baseUrl } = launchFwLiteFwLiteWeb(context);
  baseUrlHolder.baseUrl = baseUrl;
  onLaunchServerEmitter.emit(baseUrlHolder);

  const entryService = papi.networkObjects.set(
    'fwliteextension.entryService',
    new EntryService(baseUrl),
    'fw-lite-extension.IEntryService',
  );

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
      return { success: true };
    },
  );
  const openFwLiteCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.openFWLite',
    (webviewId: string) => {
      if (webviewId) {
        papi.webViews.getOpenWebViewDefinition(webviewId).then((webViewDef) => {
          const projectId = webViewDef?.projectId;
          if (projectId) {
            logger.info(
              `Opening FieldWorks Lite from web view '${webviewId}' (Platform.Bible project '${projectId}')`,
            );
            papi.projectDataProviders.get('platform.base', projectId).then((pdp) => {
              pdp.getSetting('fw-lite-extension.fwProject').then((setting) => {
                logger.info(
                  `In Platform > Settings > Project Settings, the FieldWorks project is: ${setting}`,
                );
              });
            });
          }
        });
      }

      void papi.webViews.openWebView(reactWebViewType, undefined, { existingId: '?' });
      return { success: true };
    },
  );
  const simpleFindEntryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.simpleFind',
    () => {
      onFindEntryEmitter.emit({ entry: 'apple' });
      return { success: true };
    },
  );

  const openFwProjectSelectorCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.openFWProjectSelector',
    async () => {
      logger.info('Opening FieldWorks project selector');
      await papi.webViews.openWebView(
        projectSelectWebViewType,
        { type: 'float' },
        { existingId: '?' },
      );
      return { success: true };
    },
  );
  const localProjectsCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.localProjects',
    async () => {
      logger.info('Fetching local FieldWorks projects');
      const response = await papi.fetch(`${baseUrl}${'/api/localProjects'}`);
      const jsonText = await (await response.blob()).text();
      const projects = JSON.parse(jsonText) as IProjectModel[];
      onLocalProjectsEmitter.emit({ projects });
    },
  );
  const openProjectCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.openProject',
    async (projectCode: string) => {
      logger.info(`Opening FieldWorks project: ${projectCode}`);
      onOpenProjectEmitter.emit({ projectCode });
      return { success: true };
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
    await projectSelectWebViewProviderPromise,
    await findEntryCommandPromise,
    await simpleFindEntryCommandPromise,
    await getBaseUrlCommandPromise,
    await openFwLiteCommandPromise,
    await openFwProjectSelectorCommandPromise,
    await localProjectsCommandPromise,
    await openProjectCommandPromise,
    await entryService,
    onFindEntryEmitter,
    onLaunchServerEmitter,
    onLocalProjectsEmitter,
    onOpenProjectEmitter,
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
    ['--urls', baseUrl, '--FwLiteWeb:OpenBrowser=false', '--FwLiteWeb:CorsAllowAny=true'],
    { stdio: [null, 'pipe', 'pipe'] },
  );
  if (fwLiteProcess.stdout) {
    fwLiteProcess.stdout.on('data', (data: Buffer) => {
      logger.info(`[FwLiteWeb]: ${data.toString().trim()}`);
    });
  }
  if (fwLiteProcess.stderr) {
    fwLiteProcess.stderr.on('data', (data: Buffer) => {
      logger.error(`[FwLiteWeb]: ${data.toString().trim()}`);
    });
  }
  return { baseUrl, fwLiteProcess };
}
