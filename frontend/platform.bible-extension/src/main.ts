import papi, { logger } from '@papi/backend';
import type { ExecutionActivationContext } from '@papi/core';
import type {
  FindEntryEvent,
  OpenWebViewOptionsWithProjectId,
  UrlHolder,
  WordWebViewOptions,
} from 'fw-lite-extension';
import { EntryService } from './services/entry-service';
import { WebViewType } from './types/enums';
import { FwLiteApi } from './utils/fw-lite-api';
import { ProjectManagers } from './utils/project-managers';
import * as webViewProviders from './web-views';

export async function activate(context: ExecutionActivationContext): Promise<void> {
  logger.info('FieldWorks Lite is activating!');

  /* Register web views */

  const mainWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    WebViewType.Main,
    webViewProviders.mainWebViewProvider,
  );

  const addWordWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    WebViewType.AddWord,
    webViewProviders.addWordWebViewProvider,
  );

  const dictionarySelectWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    WebViewType.DictionarySelect,
    webViewProviders.dictionarySelectWebViewProvider,
  );

  const findWordWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    WebViewType.FindWord,
    webViewProviders.findWordWebViewProvider,
  );

  /* Create event emitters */

  const onFindEntryEmitter = papi.network.createNetworkEventEmitter<FindEntryEvent>(
    'fwLiteExtension.findEntryEvent',
  );

  /* Launch FieldWorks Lite and manage the api */

  const urlHolder: UrlHolder = { baseUrl: '', dictionaryUrl: '' };
  const { fwLiteProcess, baseUrl } = launchFwLiteFwLiteWeb(context);
  urlHolder.baseUrl = baseUrl;
  const fwLiteApi = new FwLiteApi(baseUrl);

  /* Set network services */

  const entryService = papi.networkObjects.set(
    'fwliteextension.entryService',
    new EntryService(baseUrl),
    'fw-lite-extension.IEntryService',
  );

  /* Register settings validators */

  const validDictionaryCode = papi.projectSettings.registerValidator(
    'fw-lite-extension.fwDictionaryCode',
    async (dictionaryCode) => {
      if (!dictionaryCode) {
        logger.info('FieldWorks dictionary code cleared in project settings');
        return true;
      }
      logger.info('Validating FieldWorks dictionary code:', dictionaryCode);
      try {
        return !!(await fwLiteApi.getWritingSystems(dictionaryCode)).analysis;
      } catch {
        return false;
      }
    },
  );

  /* Manage project info and web views */

  const projectManagers = new ProjectManagers();

  /* Register commands */

  const getBaseUrlCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.getBaseUrl',
    () => ({ ...urlHolder }),
  );

  const addEntryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.addEntry',
    async (webViewId: string, word: string) => {
      let success = false;
      const projectManager = await projectManagers.getProjectManagerFromWebViewId(webViewId);
      if (!projectManager) return { success };
      const projectId = projectManager.projectId;
      logger.info(
        `Opening FieldWorks Lite add entry view for project '${projectId}' with word '${word}'`,
      );
      const options: WordWebViewOptions = { word };
      success = await projectManager.openWebView(WebViewType.AddWord, undefined, options);
      return { success };
    },
  );

  const browseDictionaryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.browseDictionary',
    async (webViewId: string) => {
      let success = false;
      if (!webViewId) {
        logger.warn('fwLiteExtension.browseDictionary', 'No webViewId provided');
        return { success };
      }
      const projectManager = await projectManagers.getProjectManagerFromWebViewId(webViewId);
      if (!projectManager) return { success };
      const projectId = projectManager.projectId;
      logger.info(
        `Opening FieldWorks dictionary for web view '${webViewId}' (Platform.Bible project '${projectId}')`,
      );
      const nameOrId = (await projectManager.getName()) || projectId;
      const dictionaryCode = await projectManager.getFwDictionaryCode();
      if (dictionaryCode) {
        logger.info(`Project '${nameOrId}' is using FieldWorks dictionary '${dictionaryCode}'`);
        urlHolder.dictionaryUrl = `${urlHolder.baseUrl}/paratext/fwdata/${dictionaryCode}`;
        success = await projectManager.openWebView(WebViewType.Main);
      } else {
        logger.warn(`FieldWorks dictionary not selected for project '${nameOrId}'`);
        success = await projectManager.openWebView(WebViewType.DictionarySelect, { type: 'float' });
      }
      return { success };
    },
  );

  const findEntryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.findEntry',
    async (webViewId: string, word: string) => {
      let success = false;
      const projectManager = await projectManagers.getProjectManagerFromWebViewId(webViewId);
      if (!projectManager) return { success };
      const projectId = projectManager.projectId;
      logger.info(
        `Opening FieldWorks Lite find entry view for project '${projectId}' with word '${word}'`,
      );
      const options: WordWebViewOptions = { word };
      success = await projectManager.openWebView(WebViewType.FindWord, undefined, options);
      return { success: true };
    },
  );

  // For development. Remove before publishing.
  const openFwLiteCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.openFWLite',
    async () => {
      void papi.webViews.openWebView(WebViewType.Main, undefined, { existingId: '?' });
      return { success: true };
    },
  );

  const selectFwDictionaryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.selectDictionary',
    async (projectId: string, dictionaryCode: string) => {
      logger.info(`Selecting FieldWorks dictionary '${dictionaryCode}' for project '${projectId}'`);
      const projectManager = await projectManagers.getProjectManagerFromProjectId(projectId);
      if (!projectManager) return { success: false };
      await projectManager.setFwDictionaryCode(dictionaryCode);
      return { success: true };
    },
  );

  const fwDictionariesCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.fwDictionaries',
    async () => {
      logger.info('Fetching local FieldWorks dictionaries');
      return await fwLiteApi.getProjects();
    },
  );

  // For development. Remove before publishing.
  void papi.webViews.openWebView(WebViewType.Main);

  /* Register awaited unsubscribers (do this last, to not hold up anything else) */

  context.registrations.add(
    // Web views
    await mainWebViewProviderPromise,
    await addWordWebViewProviderPromise,
    await dictionarySelectWebViewProviderPromise,
    await findWordWebViewProviderPromise,
    // Validators
    await validDictionaryCode,
    // Commands
    await addEntryCommandPromise,
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
