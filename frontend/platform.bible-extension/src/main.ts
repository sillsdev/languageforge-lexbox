import papi, { logger } from '@papi/backend';
import type {
  ExecutionActivationContext,
  IWebViewProvider,
  OpenWebViewOptions,
  SavedWebViewDefinition,
  WebViewDefinition,
} from '@papi/core';
import type {
  FindEntryEvent,
  FwDictionariesEvent,
  IProjectModel,
  LaunchServerEvent,
  OpenFwLiteEvent,
  OpenProjectEvent,
} from 'fw-lite-extension';
import fwDictionarySelect from './fw-dictionary-select.web-view?inline';
import fwLiteMainWindow from './fwLiteMainWindow.web-view?inline';
import extensionTemplateStyles from './styles.css?inline';
import { EntryService } from './entry-service';

const reactWebViewType = 'fw-lite-extension.react';
const dictionarySelectWebViewType = 'fw-dictionary-select.react';

interface OpenWebViewOptionsWithProjectId extends OpenWebViewOptions {
  projectId?: string;
}

const iconUrl = 'papi-extension://fw-lite-extension/assets/logo-dark.png';

const reactWebViewProvider: IWebViewProvider = {
  async getWebView(
    savedWebView: SavedWebViewDefinition,
    options: OpenWebViewOptionsWithProjectId,
  ): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== reactWebViewType)
      throw new Error(
        `${reactWebViewType} provider received request to provide a ${savedWebView.webViewType} web view`,
      );
    return {
      ...savedWebView,
      allowedFrameSources: ['http://localhost:*'],
      iconUrl,
      content: fwLiteMainWindow,
      projectId: options.projectId || savedWebView.projectId || undefined,
      styles: extensionTemplateStyles,
      title: '%fwLiteExtension_browseDictionary_title%',
    };
  },
};

const dictionarySelectWebViewProvider: IWebViewProvider = {
  async getWebView(
    savedWebView: SavedWebViewDefinition,
    options: OpenWebViewOptionsWithProjectId,
  ): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== dictionarySelectWebViewType)
      throw new Error(
        `${dictionarySelectWebViewType} provider received request to provide a ${savedWebView.webViewType} web view`,
      );
    return {
      ...savedWebView,
      content: fwDictionarySelect,
      iconUrl,
      projectId: options.projectId || savedWebView.projectId || undefined,
      title: '%fwLiteExtension_selectDictionary_title%',
    };
  },
};

export async function activate(context: ExecutionActivationContext): Promise<void> {
  logger.info('FieldWorks Lite is activating!');

  const reactWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    reactWebViewType,
    reactWebViewProvider,
  );

  const dictionarySelectWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    dictionarySelectWebViewType,
    dictionarySelectWebViewProvider,
  );

  // Emitter to tell subscribers how many times we have done stuff
  const onOpenFwLiteEmitter = papi.network.createNetworkEventEmitter<OpenFwLiteEvent>(
    'fwLiteExtension.openFWLite',
  );
  const onFindEntryEmitter = papi.network.createNetworkEventEmitter<FindEntryEvent>(
    'fwLiteExtension.findEntry',
  );
  const onLaunchServerEmitter = papi.network.createNetworkEventEmitter<LaunchServerEvent>(
    'fwLiteExtension.launchServer',
  );
  const onFwDictionariesEmitter = papi.network.createNetworkEventEmitter<FwDictionariesEvent>(
    'fwLiteExtension.fwDictionaries',
  );
  const onOpenProjectEmitter = papi.network.createNetworkEventEmitter<OpenProjectEvent>(
    'fwLiteExtension.openProject',
  );

  const urlHolder = { baseUrl: '', dictionaryUrl: '' };
  const { fwLiteProcess, baseUrl } = launchFwLiteFwLiteWeb(context);
  urlHolder.baseUrl = baseUrl;
  onLaunchServerEmitter.emit(urlHolder);

  const entryService = papi.networkObjects.set(
    'fwliteextension.entryService',
    new EntryService(baseUrl),
    'fw-lite-extension.IEntryService',
  );

  const getBaseUrlCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.getBaseUrl',
    () => {
      return urlHolder;
    },
  );

  const browseDictionaryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.browseDictionary',
    async (webViewId: string) => {
      if (!webViewId) {
        logger.warn('fwLiteExtension.browseDictionary', 'No webViewId provided');
        return { success: false };
      }
      const projectId = (await papi.webViews.getOpenWebViewDefinition(webViewId))?.projectId;
      if (!projectId) {
        logger.warn(
          'fwLiteExtension.browseDictionary',
          `No projectId found for web view '${webViewId}'`,
        );
        return { success: false };
      }

      logger.info(
        `Opening FieldWorks dictionary for web view '${webViewId}' (Platform.Bible project '${projectId}')`,
      );

      const pdp = await papi.projectDataProviders.get('platform.base', projectId);
      const name = (await pdp.getSetting('platform.name')) ?? projectId;
      const dictionary = await pdp.getSetting('fw-lite-extension.fwProject');
      const options: OpenWebViewOptionsWithProjectId = { existingId: '?', projectId };
      if (dictionary) {
        logger.info(`Project '${name}' is using FieldWorks dictionary '${dictionary}'`);
        urlHolder.dictionaryUrl = `${urlHolder.baseUrl}/paratext/fwdata/${dictionary}`;
        papi.webViews.openWebView(reactWebViewType, undefined, options);
      } else {
        logger.warn(`FieldWorks dictionary not selected for project '${name}'`);
        await papi.webViews.openWebView(dictionarySelectWebViewType, { type: 'float' }, options);
      }
      return { success: true };
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
    (webViewId: string) => {
      if (webViewId) {
        papi.webViews.getOpenWebViewDefinition(webViewId).then((webViewDef) => {
          const projectId = webViewDef?.projectId;
          if (projectId) {
            logger.info(
              `Opening FieldWorks Lite from web view '${webViewId}' (Platform.Bible project '${projectId}')`,
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

  const selectFwDictionaryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.selectDictionary',
    async (projectId: string, dictionaryCode: string) => {
      logger.info(`Selecting FieldWorks dictionary '${dictionaryCode}' for project '${projectId}'`);
      const pdp = await papi.projectDataProviders.get('platform.base', projectId);
      await pdp.setSetting('fw-lite-extension.fwProject', dictionaryCode);
      return { success: true };
    },
  );
  const fwDictionariesCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.fwDictionaries',
    async () => {
      logger.info('Fetching local FieldWorks dictionaries');
      const response = await papi.fetch(`${baseUrl}${'/api/localProjects'}`);
      const jsonText = await (await response.blob()).text();
      const dictionaries = JSON.parse(jsonText) as IProjectModel[];
      onFwDictionariesEmitter.emit({ dictionaries });
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
    // Web views
    await reactWebViewProviderPromise,
    await dictionarySelectWebViewProviderPromise,
    // Commands
    await browseDictionaryCommandPromise,
    await findEntryCommandPromise,
    await simpleFindEntryCommandPromise,
    await getBaseUrlCommandPromise,
    await openFwLiteCommandPromise,
    await selectFwDictionaryCommandPromise,
    await fwDictionariesCommandPromise,
    // Services
    await entryService,
    // Event emitters
    onOpenFwLiteEmitter,
    onFindEntryEmitter,
    onLaunchServerEmitter,
    onFwDictionariesEmitter,
    onOpenProjectEmitter,
    // Other cleanup
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
