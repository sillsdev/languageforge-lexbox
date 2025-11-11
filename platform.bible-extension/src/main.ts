import papi, { logger } from '@papi/backend';
import type { ExecutionActivationContext } from '@papi/core';
import type { BrowseWebViewOptions } from 'fw-lite-extension';
import { EntryService } from './services/entry-service';
import { WebViewType } from './types/enums';
import { FwLiteApi, getBrowseUrl } from './utils/fw-lite-api';
import { ProjectManagers } from './utils/project-managers';
import * as webViewProviders from './web-views';

export async function activate(context: ExecutionActivationContext): Promise<void> {
  logger.info('FieldWorks Lite is activating!');

  /* Register WebViews */

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

  const findRelatedWordsWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    WebViewType.FindRelatedWords,
    webViewProviders.findRelatedWordsWebViewProvider,
  );

  const findWordWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    WebViewType.FindWord,
    webViewProviders.findWordWebViewProvider,
  );

  /* Launch FieldWorks Lite and manage the api */

  const urlHolder = { baseUrl: '' };
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

  const validateAnalysisLanguage = papi.projectSettings.registerValidator(
    'fw-lite-extension.fwAnalysisLanguage',
    async (newValue) => !newValue || Intl.getCanonicalLocales(newValue)[0] === newValue,
  );

  const validateDictionaryCode = papi.projectSettings.registerValidator(
    'fw-lite-extension.fwDictionaryCode',
    async (newValue) => {
      if (!newValue) {
        logger.info('FieldWorks dictionary code cleared in project settings');
        return true;
      }
      logger.info('Validating FieldWorks dictionary code:', newValue);
      try {
        return !!(await fwLiteApi.getWritingSystems(newValue)).analysis;
      } catch {
        return false;
      }
    },
  );

  /* Manage project info and WebViews */

  const projectManagers = new ProjectManagers();

  /* Register commands */

  const getBaseUrlCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.getBaseUrl',
    () => urlHolder.baseUrl,
  );

  const addEntryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.addEntry',
    async (webViewId: string, word: string) => {
      let success = false;

      const projectManager = await projectManagers.getProjectManagerFromWebViewId(webViewId);
      if (!projectManager) return { success };

      const dictionaryCode = await projectManager.getFwDictionaryCodeOrOpenSelector();
      if (!dictionaryCode) return { success };

      const options = await projectManager.getWordWebViewOptions(word);
      success = await projectManager.openWebView(WebViewType.AddWord, undefined, options);
      return { success };
    },
  );

  const browseDictionaryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.browseDictionary',
    async (webViewId: string) => {
      let success = false;

      const projectManager = await projectManagers.getProjectManagerFromWebViewId(webViewId);
      if (!projectManager) return { success };

      const dictionaryCode = await projectManager.getFwDictionaryCodeOrOpenSelector();
      if (!dictionaryCode) return { success };

      const url = getBrowseUrl(urlHolder.baseUrl, dictionaryCode);
      const options: BrowseWebViewOptions = { url };
      success = await projectManager.openWebView(WebViewType.Main, undefined, options);
      return { success };
    },
  );

  const displayEntryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.displayEntry',
    async (projectId: string, entryId: string) => {
      let success = false;

      const projectManager = projectManagers.getProjectManagerFromProjectId(projectId);
      if (!projectManager) return { success };

      const dictionaryCode = await projectManager.getFwDictionaryCode();
      if (!dictionaryCode) return { success };

      logger.info(`Displaying entry '${entryId}' in FieldWorks dictionary '${dictionaryCode}'`);
      const url = getBrowseUrl(urlHolder.baseUrl, dictionaryCode, entryId);
      const options: BrowseWebViewOptions = { url };
      success = await projectManager.openWebView(WebViewType.Main, undefined, options);
      return { success };
    },
  );

  const findEntryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.findEntry',
    async (webViewId: string, word: string) => {
      let success = false;

      const projectManager = await projectManagers.getProjectManagerFromWebViewId(webViewId);
      if (!projectManager) return { success };

      const dictionaryCode = await projectManager.getFwDictionaryCodeOrOpenSelector();
      if (!dictionaryCode) return { success };

      const options = await projectManager.getWordWebViewOptions(word);
      success = await projectManager.openWebView(WebViewType.FindWord, undefined, options);
      return { success };
    },
  );

  const findRelatedEntriesCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.findRelatedEntries',
    async (webViewId: string, word: string) => {
      let success = false;

      const projectManager = await projectManagers.getProjectManagerFromWebViewId(webViewId);
      if (!projectManager) return { success };

      const dictionaryCode = await projectManager.getFwDictionaryCodeOrOpenSelector();
      if (!dictionaryCode) return { success };

      const options = await projectManager.getWordWebViewOptions(word);
      success = await projectManager.openWebView(WebViewType.FindRelatedWords, undefined, options);
      return { success };
    },
  );

  // TODO: For development; remove before publishing.
  const openFwLiteCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.openFWLite',
    async () => {
      await papi.webViews.openWebView(WebViewType.Main);
      return { success: true };
    },
  );

  const selectFwDictionaryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.selectDictionary',
    async (projectId: string, dictionaryCode: string) => {
      logger.info(`Selecting FieldWorks dictionary '${dictionaryCode}' for project '${projectId}'`);
      const projectManager = projectManagers.getProjectManagerFromProjectId(projectId);
      if (!projectManager) return { success: false };

      await projectManager.setFwDictionaryCode(dictionaryCode);
      if (dictionaryCode) {
        const langs = await fwLiteApi
          .getWritingSystems(dictionaryCode)
          .catch((e) => logger.error('Error fetching writing systems:', JSON.stringify(e)));
        const analysisLang = langs?.analysis.pop()?.wsId ?? '';
        if (analysisLang) {
          logger.info(`Storing FieldWorks dictionary analysis language '${analysisLang}'`);
        } else {
          logger.info('Failed to get analysis language of the FieldWorks dictionary');
        }
        await projectManager
          .setFwAnalysisLanguage(analysisLang)
          .catch((e) => logger.error('Error setting analysis language:', JSON.stringify(e)));
      }
      return { success: true };
    },
  );

  const fwDictionariesCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.fwDictionaries',
    async (projectId?: string) => {
      logger.info('Fetching local FieldWorks dictionaries');
      if (!projectId) return await fwLiteApi.getProjects();

      const projectManager = projectManagers.getProjectManagerFromProjectId(projectId);
      const langTag = await projectManager?.getLanguageTag();
      return await fwLiteApi.getProjectsMatchingLanguage(langTag);
    },
  );

  // TODO: For development; remove before publishing.
  papi.webViews.openWebView(WebViewType.Main, undefined, { existingId: '?' });

  /* Register awaited unsubscribers (do this last, to not hold up anything else) */

  context.registrations.add(
    // WebViews
    await mainWebViewProviderPromise,
    await addWordWebViewProviderPromise,
    await dictionarySelectWebViewProviderPromise,
    await findRelatedWordsWebViewProviderPromise,
    await findWordWebViewProviderPromise,
    // Validators
    await validateAnalysisLanguage,
    await validateDictionaryCode,
    // Commands
    await addEntryCommandPromise,
    await browseDictionaryCommandPromise,
    await displayEntryCommandPromise,
    await findEntryCommandPromise,
    await findRelatedEntriesCommandPromise,
    await getBaseUrlCommandPromise,
    await openFwLiteCommandPromise,
    await selectFwDictionaryCommandPromise,
    await fwDictionariesCommandPromise,
    // Services
    await entryService,
    // Other cleanup
    () => shutdownFwLite(fwLiteProcess),
  );

  logger.info('FieldWorks Lite is finished activating!');
}

export async function deactivate(): Promise<boolean> {
  logger.info('FieldWorks Lite is deactivating!');
  return true;
}

function launchFwLiteFwLiteWeb(context: ExecutionActivationContext) {
  const binaryPath = 'fw-lite/FwLiteWeb.exe';
  if (context.elevatedPrivileges.createProcess === undefined) {
    throw new Error('FieldWorks Lite requires createProcess elevated privileges');
  }
  if (context.elevatedPrivileges.createProcess.osData.platform !== 'win32') {
    throw new Error('FieldWorks Lite only supports launching on Windows for now');
  }
  // TODO: Instead of hardcoding the URL and port we should run it and find them in the output.
  const baseUrl = 'http://localhost:29348';

  const fwLiteProcess = context.elevatedPrivileges.createProcess.spawn(
    context.executionToken,
    binaryPath,
    ['--urls', baseUrl, '--FwLiteWeb:OpenBrowser=false', '--FwLiteWeb:CorsAllowAny=true'],
    { stdio: ['pipe', 'pipe', 'pipe'] },
  );
  fwLiteProcess.once('exit', (code, signal) => {
    logger.info(`[FwLiteWeb]: exited with code '${code}', signal '${signal}'`);
  });
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

function shutdownFwLite(
  fwLiteProcess: ReturnType<typeof launchFwLiteFwLiteWeb>['fwLiteProcess'],
): Promise<boolean> {
  return new Promise((resolve) => {
    // Already exited?
    // eslint-disable-next-line no-null/no-null
    if (fwLiteProcess.exitCode !== null) {
      resolve(fwLiteProcess.exitCode === 0);
      return;
    }

    let forced = false;
    const killTimer = setTimeout(() => {
      forced = true;
      try {
        fwLiteProcess.kill('SIGKILL');
      } catch (e) {
        logger.error(`[FwLiteWeb]: forced kill failed: ${e}`);
      }
    }, 10000);

    fwLiteProcess.once('exit', (code, signal) => {
      clearTimeout(killTimer);
      if (code === 0 || forced) {
        resolve(true);
      } else {
        logger.error(`[FwLiteWeb]: shutdown failed with code '${code}', signal '${signal}'`);
        resolve(false);
      }
    });
    fwLiteProcess.once('error', (error) => {
      clearTimeout(killTimer);
      logger.error(`[FwLiteWeb]: shutdown failed with error: ${error}`);
      resolve(false);
    });

    // Guard stdin operations
    if (fwLiteProcess.stdin && !fwLiteProcess.stdin.destroyed) {
      try {
        fwLiteProcess.stdin.write('shutdown\n');
        fwLiteProcess.stdin.end();
      } catch (e) {
        logger.error(`[FwLiteWeb]: stdin write failed: ${e}`);
        // Fall back to immediate forced kill
        forced = true;
        clearTimeout(killTimer);
        try {
          fwLiteProcess.kill('SIGKILL');
        } catch (killError) {
          logger.error(`[FwLiteWeb]: forced kill failed: ${killError}`);
        }
      }
    } else {
      // No stdin available, force kill immediately
      logger.warn('[FwLiteWeb]: stdin unavailable, forcing immediate kill');
      forced = true;
      clearTimeout(killTimer);
      try {
        fwLiteProcess.kill('SIGKILL');
      } catch (e) {
        logger.error(`[FwLiteWeb]: forced kill failed: ${e}`);
      }
    }
  });
}
