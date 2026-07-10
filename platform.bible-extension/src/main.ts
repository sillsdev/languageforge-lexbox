import papi, { logger } from '@papi/backend';
import type { ExecutionActivationContext } from '@papi/core';
import { ChildProcessByStdio } from 'child_process';
import type { BrowseWebViewOptions } from 'lexicon';
import { getErrorMessage } from 'platform-bible-utils';
import { Stream } from 'stream';
import { EntryService } from './services/entry-service';
import { WebViewType } from './types/enums';
import { FwLiteApi, type LoginResult } from './utils/fw-lite-api';
import { ProjectManagers } from './utils/project-managers';
import * as webViewProviders from './web-views';

let fwLiteProcess: ChildProcessByStdio<Stream.Writable, Stream.Readable, Stream.Readable>;

// Signing in can easily take longer than 30s, set increase to 5min. The stack of timeouts seems to be:
//  5 min - papi command (this one)
//  5 min - undici/node fetch (has gone back and forth; 300s today, see https://github.com/nodejs/undici/pull/5467)
//  inf.  - FW Lite
const SIGN_IN_TIMEOUT_MS = 5 * 60 * 1000;

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
        return (await fwLiteApi.getWritingSystems(newValue)).vernacular.length > 0;
      } catch {
        return false;
      }
    },
  );

  /* Manage project info and WebViews */

  const projectManagers = new ProjectManagers();

  /* Register commands */

  const getAuthServers = async () => {
    try {
      return await fwLiteApi.getAuthServers();
    } catch (e) {
      logger.error('Error fetching Lexbox auth servers:', getErrorMessage(e));
      return undefined;
    }
  };

  const addEntryCommandPromise = papi.commands.registerCommand(
    'lexicon.addEntry',
    async (webViewId: string, word: string) => {
      let success = false;

      const projectManager =
        await projectManagers.getProjectManagerFromWebViewIdOrSelectProject(webViewId);
      if (!projectManager) return { success };

      const lexiconCode = await projectManager.getLexiconCodeOrOpenSelector();
      if (!lexiconCode) return { success };

      const options = await projectManager.getLexiconWebViewOptions(word);
      success = await projectManager.openWebView(WebViewType.AddWord, undefined, options);
      return { success };
    },
  );

  const authServersCommandPromise = papi.commands.registerCommand(
    'lexicon.authServers',
    getAuthServers,
  );

  const browseLexiconCommandPromise = papi.commands.registerCommand(
    'lexicon.browseLexicon',
    async (webViewId: string) => {
      let success = false;

      const projectManager =
        await projectManagers.getProjectManagerFromWebViewIdOrSelectProject(webViewId);
      if (!projectManager) return { success };

      const lexiconCode = await projectManager.getLexiconCodeOrOpenSelector();
      if (!lexiconCode) return { success };

      const url = await fwLiteApi.getBrowseUrl(lexiconCode);
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
      const url = await fwLiteApi.getBrowseUrl(lexiconCode, entryId);
      const options: BrowseWebViewOptions = { url };
      success = await projectManager.openWebView(WebViewType.Main, undefined, options);
      return { success };
    },
  );

  const findEntryCommandPromise = papi.commands.registerCommand(
    'lexicon.findEntry',
    async (webViewId: string, word: string) => {
      let success = false;

      const projectManager =
        await projectManagers.getProjectManagerFromWebViewIdOrSelectProject(webViewId);
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

      const projectManager =
        await projectManagers.getProjectManagerFromWebViewIdOrSelectProject(webViewId);
      if (!projectManager) return { success };

      const lexiconCode = await projectManager.getLexiconCodeOrOpenSelector();
      if (!lexiconCode) return { success };

      const options = await projectManager.getLexiconWebViewOptions(word);
      success = await projectManager.openWebView(WebViewType.FindRelatedWords, undefined, options);
      return { success };
    },
  );

  const loginCommandPromise = papi.commands.registerCommand(
    'lexicon.login',
    async (authority: string) => {
      let result: LoginResult | undefined;
      // Abort the backend sign-in once the command times out, so an abandoned sign-in doesn't
      // linger on FW Lite (login-web-view cancels via HttpContext.RequestAborted).
      const abort = new AbortController();
      const timeout = setTimeout(() => abort.abort(), SIGN_IN_TIMEOUT_MS);
      try {
        result = await fwLiteApi.login(authority, abort.signal);
      } catch (e) {
        logger.error('Error signing in to Lexbox:', getErrorMessage(e));
      } finally {
        clearTimeout(timeout);
      }
      return { result, servers: await getAuthServers() };
    },
    undefined,
    { timeoutMilliseconds: SIGN_IN_TIMEOUT_MS },
  );

  const logoutCommandPromise = papi.commands.registerCommand(
    'lexicon.logout',
    async (authority: string) => {
      try {
        await fwLiteApi.logout(authority);
      } catch (e) {
        logger.error('Error signing out of Lexbox:', getErrorMessage(e));
        throw e; // Surface the failure so the web view can flag it instead of silently re-enabling.
      }
      return getAuthServers();
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

  const createLexiconCommandPromise = papi.commands.registerCommand(
    'lexicon.createLexicon',
    async (name: string, code: string, vernacularWs: string, analysisWs?: string) => {
      try {
        await fwLiteApi.createProject(name, code, vernacularWs, analysisWs);
        return { success: true };
      } catch (e) {
        const error = e instanceof Error ? e.message : String(e);
        logger.error('Error creating lexicon:', error);
        return { success: false, error };
      }
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
    await authServersCommandPromise,
    await browseLexiconCommandPromise,
    await createLexiconCommandPromise,
    await displayEntryCommandPromise,
    await findEntryCommandPromise,
    await findRelatedEntriesCommandPromise,
    await lexiconsCommandPromise,
    await loginCommandPromise,
    await logoutCommandPromise,
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

/**
 * Returns a stable per-user directory for FW Lite data (projects, auth cache), in its own
 * subdirectory so it doesn't collide with Platform.Bible's own `papi.storage` data for this
 * extension (`.../extensions/lexicon/user-data/`). Mirrors Platform.Bible's own `app://` scheme
 * (paranext-core's `getAppDir()`): the real per-user location when packaged, the repo-local
 * dev-appdata directory in development, so `npm start` doesn't read/write production user data.
 *
 * Uses process.env/globalThis instead of require('os'/'path') because Platform.Bible blocks
 * non-papi requires, so paths are assembled by hand with the platform-appropriate separator
 * (backslash on Windows, forward slash on Linux/Mac, which .NET requires there).
 */
function getFwLiteDataDir(platform: string): string {
  const isWindows = platform === 'win32';
  const sep = isWindows ? '\\' : '/';
  let appDataDir: string;
  if (globalThis.isPackaged) {
    // Mirrors paranext-core's os.homedir()
    const home = isWindows ? process.env.USERPROFILE : process.env.HOME;
    if (!home) {
      const homeVar = isWindows ? 'USERPROFILE' : 'HOME';
      throw new Error(`Cannot determine FW Lite data directory: ${homeVar} is not set`);
    }
    appDataDir = `${home}${sep}.platform.bible`;
  } else {
    appDataDir = `${globalThis.resourcesPath}${sep}dev-appdata`;
  }
  return `${appDataDir}${sep}extensions${sep}lexicon${sep}fw-lite`;
}

/**
 * Returns the extension-relative path to the FW Lite binary. Forward slashes on all platforms:
 * createProcess (Node) resolves it, so unlike getFwLiteDataDir it needs no Windows separator.
 */
function getFwLiteBinaryPath(platform: string): string {
  switch (platform) {
    case 'win32':
      return 'fw-lite/win-x64/FwLiteWeb.exe';
    case 'linux':
      // The extension zip doesn't preserve the Unix executable bit, but paranext-core's
      // createProcess.spawn sets it on the command before spawning, so a plain spawn works.
      return 'fw-lite/linux-x64/FwLiteWeb';
    default:
      // macOS is out of scope for now: https://github.com/sillsdev/languageforge-lexbox/issues/1603
      throw new Error(`Cannot launch FW Lite on unsupported platform '${platform}'`);
  }
}

/** Launches the FieldWorks Lite process and returns its URL domain. */
function launchFwLite(context: ExecutionActivationContext): string {
  if (context.elevatedPrivileges.createProcess === undefined) {
    throw new Error('Requires createProcess elevated privileges to launch FW Lite');
  }
  const { platform } = context.elevatedPrivileges.createProcess.osData;
  const binaryPath = getFwLiteBinaryPath(platform);
  // TODO: Instead of hardcoding the URL and port we should run it and find them in the output.
  const baseUrl = 'http://localhost:29348';

  const dataDir = getFwLiteDataDir(platform);
  const sep = platform === 'win32' ? '\\' : '/';
  const authCacheFile = `${dataDir}${sep}msal.json`;
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
      `--LcmCrdt:ProjectPath=${dataDir}`,
      `--Auth:CacheFileName=${authCacheFile}`,
      // Sign in via the user's default browser: an embedded login would be blocked by the
      // webview sandbox and/or Lexbox's frame-ancestors CSP.
      '--Auth:SystemWebViewLogin=true',
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
