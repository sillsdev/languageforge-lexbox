import papi, { logger } from '@papi/backend';
import type { ExecutionActivationContext } from '@papi/core';
import { ChildProcessByStdio } from 'child_process';
import type { BrowseWebViewOptions } from 'fw-lite-extension';
import { Stream } from 'stream';
import { EntryService } from './services/entry-service';
import { WebViewType } from './types/enums';
import { FwLiteApi, getBrowseUrl } from './utils/fw-lite-api';
import { ProjectManagers } from './utils/project-managers';
import * as webViewProviders from './web-views';

let baseUrl: string;

let fwLiteProcess: ChildProcessByStdio<Stream.Writable, Stream.Readable, Stream.Readable>;

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

  launchFwLiteWeb(context);
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

  const addEntryCommandPromise = papi.commands.registerCommand(
    'fwLiteExtension.addEntry',
    async (webViewId: string, word: string) => {
      let success = false;

      const projectManager = await projectManagers.getProjectManagerFromWebViewId(webViewId);
      if (!projectManager) return { success };

      const dictionaryCode = await projectManager.getFwDictionaryCodeOrOpenSelector();
      if (!dictionaryCode) return { success };

      const options = await projectManager.getDictionaryWebViewOptions(word);
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

      const url = getBrowseUrl(baseUrl, dictionaryCode);
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
      const url = getBrowseUrl(baseUrl, dictionaryCode, entryId);
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

      const options = await projectManager.getDictionaryWebViewOptions(word);
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

      const options = await projectManager.getDictionaryWebViewOptions(word);
      success = await projectManager.openWebView(WebViewType.FindRelatedWords, undefined, options);
      return { success };
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
        const analysisLang = langs?.analysis[0]?.wsId ?? '';
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
    await selectFwDictionaryCommandPromise,
    await fwDictionariesCommandPromise,
    // Services
    await entryService,
  );

  logger.info('FieldWorks Lite is finished activating!');
}

export async function deactivate(): Promise<boolean> {
  logger.info('FieldWorks Lite is deactivating!');
  return await shutDownFwLite();
}

function launchFwLiteWeb(context: ExecutionActivationContext) {
  const binaryPath = 'fw-lite/FwLiteWeb.exe';
  if (context.elevatedPrivileges.createProcess === undefined) {
    throw new Error('FieldWorks Lite requires createProcess elevated privileges');
  }
  if (context.elevatedPrivileges.createProcess.osData.platform !== 'win32') {
    throw new Error('FieldWorks Lite only supports launching on Windows for now');
  }
  // TODO: Instead of hardcoding the URL and port we should run it and find them in the output.
  baseUrl = 'http://localhost:29348';

  fwLiteProcess = context.elevatedPrivileges.createProcess.spawn(
    context.executionToken,
    binaryPath,
    [
      '--urls',
      baseUrl,
      '--FwLite:UpdateCheckCondition=Never',
      '--FwLiteWeb:CorsAllowAny=true',
      '--FwLiteWeb:OpenBrowser=false',
    ],
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
}

function shutDownFwLite(): Promise<boolean> {
  return new Promise((resolve) => {
    logger.info('[FwLiteWeb]: shutting down process');

    let shutdownResolved = false;
    let timeoutId: ReturnType<typeof setTimeout> | undefined;

    function resolveShutdown(success: boolean) {
      clearTimeout(timeoutId);
      timeoutId = undefined;
      if (shutdownResolved) return;
      shutdownResolved = true;
      resolve(success);
    }

    function resolveIfExited(): boolean {
      // eslint-disable-next-line no-null/no-null
      if (fwLiteProcess.exitCode === null) {
        return false;
      }

      logger.info('[FwLiteWeb]: process already exited');
      resolveShutdown(fwLiteProcess.exitCode === 0);
      return true;
    }

    resolveIfExited();

    function killProcess(reason: string) {
      logger.info('[FwLiteWeb]: killing process', reason);
      if (resolveIfExited()) return;

      const killed = fwLiteProcess.kill('SIGKILL');
      if (!killed) {
        logger.error('[FwLiteWeb]: failed to kill process', reason);
        resolveShutdown(false);
      } else {
        logger.warn('[FwLiteWeb]: force killed process', reason);
        resolveShutdown(true);
      }
    }

    fwLiteProcess.once('exit', (code, signal) => {
      if (code === 0) {
        logger.info('[FwLiteWeb]: shutdown successful');
        resolveShutdown(true);
      } else {
        logger.error(`[FwLiteWeb]: shutdown failed with code '${code}', signal '${signal}'`);
        resolveShutdown(false);
      }
    });

    fwLiteProcess.once('error', (error) => {
      logger.error('[FwLiteWeb]: shutdown failed with error', error);
      // only kill if we're not waiting for a graceful shutdown
      if (!timeoutId) killProcess('on error');
    });

    if (!fwLiteProcess.stdin) {
      logger.error('[FwLiteWeb]: shutdown failed because stdin is unavailable');
      killProcess('because stdin is unavailable');
      return;
    }

    try {
      fwLiteProcess.stdin.write('shutdown\n');
      fwLiteProcess.stdin.end();
      timeoutId = setTimeout(() => {
        killProcess('after shutdown timeout');
      }, 1400); // On shutdown, the extension host only waits 1.5 seconds before force killing us.
    } catch (error) {
      logger.error('[FwLiteWeb]: failed to send shutdown command', error);
      killProcess('after failed shutdown command');
    }
  });
}
