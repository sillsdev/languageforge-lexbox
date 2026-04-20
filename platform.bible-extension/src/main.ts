import papi, { logger } from '@papi/backend';
import type { ExecutionActivationContext } from '@papi/core';
import { ChildProcessByStdio } from 'child_process';
import type { BrowseWebViewOptions } from 'lexicon';
import { Stream } from 'stream';
import { EntryService } from './services/entry-service';
import { WebViewType } from './types/enums';
import { FwLiteApi, getBrowseUrl } from './utils/fw-lite-api';
import { ProjectManagers } from './utils/project-managers';
import * as webViewProviders from './web-views';

let fwLiteProcess: ChildProcessByStdio<Stream.Writable, Stream.Readable, Stream.Readable>;

export async function activate(context: ExecutionActivationContext): Promise<void> {
  logger.info('Lexicon extension activating!');

  /* Register WebViews */

  const mainWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    WebViewType.Main,
    webViewProviders.mainWebViewProvider,
  );

  const addWordWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    WebViewType.AddWord,
    webViewProviders.addWordWebViewProvider,
  );

  const findRelatedWordsWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    WebViewType.FindRelatedWords,
    webViewProviders.findRelatedWordsWebViewProvider,
  );

  const findWordWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    WebViewType.FindWord,
    webViewProviders.findWordWebViewProvider,
  );

  const selectLexiconWebViewProviderPromise = papi.webViewProviders.registerWebViewProvider(
    WebViewType.SelectLexicon,
    webViewProviders.selectLexiconWebViewProvider,
  );

  /* Launch FieldWorks Lite and manage the api */

  const baseUrl = launchFwLite(context);
  const fwLiteApi = new FwLiteApi(baseUrl);

  /* Set network services */

  const entryService = papi.networkObjects.set(
    'lexicon.entryService',
    new EntryService(baseUrl),
    'lexicon.IEntryService',
  );

  /* Register settings validators */

  const validateAnalysisLanguage = papi.projectSettings.registerValidator(
    'lexicon.analysisLanguage',
    async (newValue) => !newValue || Intl.getCanonicalLocales(newValue)[0] === newValue,
  );

  const validateLexiconCode = papi.projectSettings.registerValidator(
    'lexicon.lexiconCode',
    async (newValue) => {
      if (!newValue) {
        logger.info('Lexicon code cleared in project settings');
        return true;
      }
      logger.info('Validating lexicon code:', newValue);
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
    'lexicon.addEntry',
    async (webViewId: string, word: string) => {
      let success = false;

      const projectManager = await projectManagers.getProjectManagerFromWebViewId(webViewId);
      if (!projectManager) return { success };

      const lexiconCode = await projectManager.getLexiconCodeOrOpenSelector();
      if (!lexiconCode) return { success };

      const options = await projectManager.getLexiconWebViewOptions(word);
      success = await projectManager.openWebView(WebViewType.AddWord, undefined, options);
      return { success };
    },
  );

  const browseLexiconCommandPromise = papi.commands.registerCommand(
    'lexicon.browseLexicon',
    async (webViewId: string) => {
      let success = false;

      const projectManager = await projectManagers.getProjectManagerFromWebViewId(webViewId);
      if (!projectManager) return { success };

      const lexiconCode = await projectManager.getLexiconCodeOrOpenSelector();
      if (!lexiconCode) return { success };

      const url = getBrowseUrl(baseUrl, lexiconCode);
      const options: BrowseWebViewOptions = { url };
      success = await projectManager.openWebView(WebViewType.Main, undefined, options);
      return { success };
    },
  );

  const displayEntryCommandPromise = papi.commands.registerCommand(
    'lexicon.displayEntry',
    async (projectId: string, entryId: string) => {
      let success = false;

      const projectManager = projectManagers.getProjectManagerFromProjectId(projectId);
      if (!projectManager) return { success };

      const lexiconCode = await projectManager.getLexiconCode();
      if (!lexiconCode) return { success };

      logger.info(`Displaying entry '${entryId}' in lexicon '${lexiconCode}'`);
      const url = getBrowseUrl(baseUrl, lexiconCode, entryId);
      const options: BrowseWebViewOptions = { url };
      success = await projectManager.openWebView(WebViewType.Main, undefined, options);
      return { success };
    },
  );

  const findEntryCommandPromise = papi.commands.registerCommand(
    'lexicon.findEntry',
    async (webViewId: string, word: string) => {
      let success = false;

      const projectManager = await projectManagers.getProjectManagerFromWebViewId(webViewId);
      if (!projectManager) return { success };

      const lexiconCode = await projectManager.getLexiconCodeOrOpenSelector();
      if (!lexiconCode) return { success };

      const options = await projectManager.getLexiconWebViewOptions(word);
      success = await projectManager.openWebView(WebViewType.FindWord, undefined, options);
      return { success };
    },
  );

  const findRelatedEntriesCommandPromise = papi.commands.registerCommand(
    'lexicon.findRelatedEntries',
    async (webViewId: string, word: string) => {
      let success = false;

      const projectManager = await projectManagers.getProjectManagerFromWebViewId(webViewId);
      if (!projectManager) return { success };

      const lexiconCode = await projectManager.getLexiconCodeOrOpenSelector();
      if (!lexiconCode) return { success };

      const options = await projectManager.getLexiconWebViewOptions(word);
      success = await projectManager.openWebView(WebViewType.FindRelatedWords, undefined, options);
      return { success };
    },
  );

  const selectLexiconCommandPromise = papi.commands.registerCommand(
    'lexicon.selectLexicon',
    async (projectId: string, lexiconCode: string) => {
      logger.info(`Selecting lexicon '${lexiconCode}' for project '${projectId}'`);
      const projectManager = projectManagers.getProjectManagerFromProjectId(projectId);
      if (!projectManager) return { success: false };

      await projectManager.setLexiconCode(lexiconCode);
      if (lexiconCode) {
        const langs = await fwLiteApi
          .getWritingSystems(lexiconCode)
          .catch((e) => logger.error('Error fetching writing systems:', JSON.stringify(e)));
        const analysisLang = langs?.analysis[0]?.wsId ?? '';
        if (analysisLang) {
          logger.info(`Storing lexicon analysis language '${analysisLang}'`);
        } else {
          logger.info('Failed to get analysis language of the lexicon');
        }
        await projectManager
          .setAnalysisLanguage(analysisLang)
          .catch((e) => logger.error('Error setting analysis language:', JSON.stringify(e)));
      }
      return { success: true };
    },
  );

  const lexiconsCommandPromise = papi.commands.registerCommand(
    'lexicon.lexicons',
    async (projectId?: string) => {
      logger.info('Fetching local lexicons');
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
    await findRelatedWordsWebViewProviderPromise,
    await findWordWebViewProviderPromise,
    await selectLexiconWebViewProviderPromise,
    // Validators
    await validateAnalysisLanguage,
    await validateLexiconCode,
    // Commands
    await addEntryCommandPromise,
    await browseLexiconCommandPromise,
    await displayEntryCommandPromise,
    await findEntryCommandPromise,
    await findRelatedEntriesCommandPromise,
    await lexiconsCommandPromise,
    await selectLexiconCommandPromise,
    // Services
    await entryService,
  );

  logger.info('Lexicon extension finished activating!');
}

export async function deactivate(): Promise<boolean> {
  logger.info('Lexicon extension deactivating!');
  return await shutDownFwLite();
}

/** Launches the FieldWorks Lite process and returns its URL domain. */
function launchFwLite(context: ExecutionActivationContext): string {
  const binaryPath = 'fw-lite/FwLiteWeb.exe';
  if (context.elevatedPrivileges.createProcess === undefined) {
    throw new Error('Requires createProcess elevated privileges to launch FW Lite');
  }
  if (context.elevatedPrivileges.createProcess.osData.platform !== 'win32') {
    throw new Error('Requires Windows to launch FW Lite');
  }
  // TODO: Instead of hardcoding the URL and port we should run it and find them in the output.
  const baseUrl = 'http://localhost:29348';

  fwLiteProcess = context.elevatedPrivileges.createProcess.spawn(
    context.executionToken,
    binaryPath,
    [
      '--urls',
      baseUrl,
      '--FwLite:UpdateCheckCondition=Never',
      '--FwLiteWeb:CorsAllowAny=true',
      '--FwLiteWeb:EnableFileLogging=false', // already piped to P.B (and triggers npm watch)
      '--FwLiteWeb:OpenBrowser=false',
    ],
    { stdio: ['pipe', 'pipe', 'pipe'] },
  );
  fwLiteProcess.once('exit', (code, signal) => {
    logger.info(`[FwLite]: exited with code '${code}', signal '${signal}'`);
  });
  if (fwLiteProcess.stdout) {
    fwLiteProcess.stdout.on('data', (data: Buffer) => {
      logger.info(`[FwLite]: ${data.toString().trim()}`);
    });
  }
  if (fwLiteProcess.stderr) {
    fwLiteProcess.stderr.on('data', (data: Buffer) => {
      logger.error(`[FwLite]: ${data.toString().trim()}`);
    });
  }

  return baseUrl;
}

function shutDownFwLite(): Promise<boolean> {
  return new Promise((resolve) => {
    logger.info('[FwLite]: shutting down process');

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

      logger.info('[FwLite]: process already exited');
      resolveShutdown(fwLiteProcess.exitCode === 0);
      return true;
    }

    resolveIfExited();

    function killProcess(reason: string) {
      logger.info('[FwLite]: killing process', reason);
      if (resolveIfExited()) return;

      const killed = fwLiteProcess.kill('SIGKILL');
      if (!killed) {
        logger.error('[FwLite]: failed to kill process', reason);
        resolveShutdown(false);
      } else {
        logger.warn('[FwLite]: force killed process', reason);
        resolveShutdown(true);
      }
    }

    fwLiteProcess.once('exit', (code, signal) => {
      if (code === 0) {
        logger.info('[FwLite]: shutdown successful');
        resolveShutdown(true);
      } else {
        logger.error(`[FwLite]: shutdown failed with code '${code}', signal '${signal}'`);
        resolveShutdown(false);
      }
    });

    fwLiteProcess.once('error', (error) => {
      logger.error('[FwLite]: shutdown failed with error', error);
      // Only kill if we're not waiting for a graceful shutdown.
      if (!timeoutId) killProcess('on error');
    });

    if (!fwLiteProcess.stdin) {
      logger.error('[FwLite]: shutdown failed because stdin is unavailable');
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
      logger.error('[FwLite]: failed to send shutdown command', error);
      killProcess('after failed shutdown command');
    }
  });
}
