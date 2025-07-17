import papi, { logger } from '@papi/backend';
import type {
  ExecutionActivationContext,
  IWebViewProvider,
  SavedWebViewDefinition,
  WebViewDefinition,
} from '@papi/core';
import type {
  FindEntryEvent,
  FindWebViewOptions,
  IProjectModel,
  IWritingSystems,
  OpenWebViewOptionsWithProjectId,
  UrlHolder,
} from 'fw-lite-extension';
import fwDictionarySelect from './fw-dictionary-select.web-view?inline';
import fwFindWindow from './fwlite-find-word.web-view?inline';
import fwLiteMainWindow from './fwLiteMainWindow.web-view?inline';
import extensionTemplateStyles from './styles.css?inline';
import { EntryService } from './entry-service';

const mainWebViewType = 'fw-lite-extension.react';
const dictionarySelectWebViewType = 'fw-dictionary-select.react';
const findWordWebViewType = 'fwlite-find-word.react';

const iconUrl = 'papi-extension://fw-lite-extension/assets/logo-dark.png';

const mainWebViewProvider: IWebViewProvider = {
  async getWebView(
    savedWebView: SavedWebViewDefinition,
    options: OpenWebViewOptionsWithProjectId,
  ): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== mainWebViewType)
      throw new Error(
        `${mainWebViewType} provider received request to provide a ${savedWebView.webViewType} web view`,
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

const findWordWebViewProvider: IWebViewProvider = {
  async getWebView(
    savedWebView: SavedWebViewDefinition,
    options: FindWebViewOptions,
  ): Promise<WebViewDefinition | undefined> {
    if (savedWebView.webViewType !== findWordWebViewType)
      throw new Error(
        `${findWordWebViewType} provider received request to provide a ${savedWebView.webViewType} web view`,
      );
    return {
      ...savedWebView,
      ...options,
      content: fwFindWindow,
      iconUrl,
      title: '%fwLiteExtension_findWord_title%',
    };
  },
};

export async function activate(context: ExecutionActivationContext): Promise<void> {
  logger.info('FieldWorks Lite is activating!');

  const mainWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    mainWebViewType,
    mainWebViewProvider,
  );
  const dictionarySelectWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    dictionarySelectWebViewType,
    dictionarySelectWebViewProvider,
  );
  const findWordWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    findWordWebViewType,
    findWordWebViewProvider,
  );

  const onFindEntryEmitter = papi.network.createNetworkEventEmitter<FindEntryEvent>(
    'fwLiteExtension.findEntryEvent',
  );

  const urlHolder: UrlHolder = { baseUrl: '', dictionaryUrl: '' };
  const { fwLiteProcess, baseUrl } = launchFwLiteFwLiteWeb(context);
  urlHolder.baseUrl = baseUrl;

  async function miniLcmApiFetch(path: string): Promise<string | undefined> {
    const apiUrl = `${baseUrl}/api/${path}`;
    try {
      return await (await papi.fetch(apiUrl)).text();
    } catch (e) {
      logger.error(`Error fetching ${apiUrl} from MiniLCM API`, e);
    }
  }

  const entryService = papi.networkObjects.set(
    'fwliteextension.entryService',
    new EntryService(baseUrl),
    'fw-lite-extension.IEntryService',
  );

  const validDictionaryCode = papi.projectSettings.registerValidator(
    'fw-lite-extension.fwDictionaryCode',
    async (dictionaryCode) => {
      if (!dictionaryCode) {
        logger.info('FieldWorks dictionary code cleared in project settings');
        return true;
      }

      // TODO: Sanitize dictionaryCode

      logger.info('Validating FieldWorks dictionary code:', dictionaryCode);
      const jsonText = await miniLcmApiFetch(`mini-lcm/FwData/${dictionaryCode}/writingSystems`);
      return jsonText ? !!(JSON.parse(jsonText) as IWritingSystems).analysis : false;
    },
  );

  const getBaseUrlCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.getBaseUrl',
    () => ({ ...urlHolder }),
  );

  async function getProjectIdFromWebViewId(webViewId: string): Promise<string | undefined> {
    const webViewDef = await papi.webViews
      .getOpenWebViewDefinition(webViewId)
      .catch((e) => void logger.error('Error getting web view definition:', e));
    if (!webViewDef?.projectId) {
      logger.warn(`No projectId found for web view '${webViewId}'`);
      return undefined;
    }
    return webViewDef.projectId;
  }

  async function getFwProjectSetting(webViewId: string): Promise<string | undefined> {
    const projectId = await getProjectIdFromWebViewId(webViewId);
    if (!projectId) return undefined;
    const pdp = await papi.projectDataProviders.get('platform.base', projectId);
    return pdp.getSetting('fw-lite-extension.fwDictionaryCode');
  }

  const browseDictionaryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.browseDictionary',
    async (webViewId: string) => {
      if (!webViewId) {
        logger.warn('fwLiteExtension.browseDictionary', 'No webViewId provided');
        return { success: false };
      }
      const projectId = await getProjectIdFromWebViewId(webViewId);
      if (!projectId) return { success: false };

      logger.info(
        `Opening FieldWorks dictionary for web view '${webViewId}' (Platform.Bible project '${projectId}')`,
      );

      const pdp = await papi.projectDataProviders.get('platform.base', projectId);
      const name = (await pdp.getSetting('platform.name')) ?? projectId;
      const dictionary = await pdp.getSetting('fw-lite-extension.fwDictionaryCode');
      const options: OpenWebViewOptionsWithProjectId = { existingId: '?', projectId };
      if (dictionary) {
        logger.info(`Project '${name}' is using FieldWorks dictionary '${dictionary}'`);
        urlHolder.dictionaryUrl = `${urlHolder.baseUrl}/paratext/fwdata/${dictionary}`;
        await papi.webViews.openWebView(mainWebViewType, undefined, options);
      } else {
        logger.warn(`FieldWorks dictionary not selected for project '${name}'`);
        await papi.webViews.openWebView(dictionarySelectWebViewType, { type: 'float' }, options);
      }
      return { success: true };
    },
  );
  const findEntryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.findEntry',
    async (webViewId: string, word: string) => {
      const projectId = await getProjectIdFromWebViewId(webViewId);
      if (!projectId) return { success: false };
      logger.info(
        `Opening FieldWorks Lite find entry view for project '${projectId}' with word '${word}'`,
      );
      const options: FindWebViewOptions = { existingId: '?', projectId, word };
      void papi.webViews.openWebView(findWordWebViewType, undefined, options);
      return { success: true };
    },
  );
  const openFwLiteCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.openFWLite',
    async (webViewId: string) => {
      const projectId = webViewId
        ? (await papi.webViews.getOpenWebViewDefinition(webViewId))?.projectId
        : undefined;
      const options: OpenWebViewOptionsWithProjectId = { existingId: '?', projectId };
      void papi.webViews.openWebView(mainWebViewType, undefined, options);
      return { success: true };
    },
  );
  const selectFwDictionaryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.selectDictionary',
    async (projectId: string, dictionaryCode: string) => {
      logger.info(`Selecting FieldWorks dictionary '${dictionaryCode}' for project '${projectId}'`);
      const pdp = await papi.projectDataProviders.get('platform.base', projectId);
      await pdp.setSetting('fw-lite-extension.fwDictionaryCode', dictionaryCode);
      return { success: true };
    },
  );
  const fwDictionariesCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.fwDictionaries',
    async () => {
      logger.info('Fetching local FieldWorks dictionaries');
      const jsonText = await miniLcmApiFetch('localProjects');
      return jsonText ? (JSON.parse(jsonText) as IProjectModel[]) : [];
    },
  );

  // Create WebViews or get an existing WebView if one already exists for this type
  // Note: here, we are using `existingId: '?'` to indicate we do not want to create a new WebView
  // if one already exists. The WebView that already exists could have been created by anyone
  // anywhere; it just has to match `webViewType`. See `paranext-core's hello-someone.ts` for an
  // example of keeping an existing WebView that was specifically created by
  // `paranext-core's hello-someone`.
  void papi.webViews.openWebView(mainWebViewType, undefined, { existingId: '?' });

  // Await the registration promises at the end so we don't hold everything else up
  context.registrations.add(
    // Web views
    await mainWebViewProviderPromise,
    await dictionarySelectWebViewProviderPromise,
    await findWordWebViewProviderPromise,
    // Validators
    await validDictionaryCode,
    // Commands
    await browseDictionaryCommandPromise,
    await findEntryCommandPromise,
    await getBaseUrlCommandPromise,
    await openFwLiteCommandPromise,
    await selectFwDictionaryCommandPromise,
    await fwDictionariesCommandPromise,
    // Services
    await entryService,
    // Event emitters
    onFindEntryEmitter,
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
