import papi, { logger } from '@papi/backend';
import type { ExecutionActivationContext } from '@papi/core';
import { ChildProcessByStdio } from 'child_process';
import type { BrowseWebViewOptions } from 'lexicon';
import { getErrorMessage } from 'platform-bible-utils';
import { Stream } from 'stream';
import { EntryService } from './services/entry-service';
import { WebViewType } from './types/enums';
import { type DownloadResult, FwLiteApi, type LoginResult } from './utils/fw-lite-api';
import { HttpStatusError } from './utils/http-status-error';
import type { ProjectManager } from './utils/project-manager';
import { ProjectManagers } from './utils/project-managers';
import * as webViewProviders from './web-views';

let fwLiteProcess: ChildProcessByStdio<Stream.Writable, Stream.Readable, Stream.Readable>;

// Signing in can easily take longer than 30s. Timeout stack: this papi command (5 min) -> undici/
// node fetch (5 min, see https://github.com/nodejs/undici/pull/5467) -> FW Lite (no timeout).
const SIGN_IN_TIMEOUT_MS = 5 * 60 * 1000;

// Downloading a project runs its full initial sync inline, which for a large project is minutes.
// Same timeout stack as sign-in (see above).
const DOWNLOAD_TIMEOUT_MS = 5 * 60 * 1000;

// Resolving a project can open the core project picker and wait on the user. Same 5-min budget as
// sign-in/download — arbitrary, since it only caps an abandoned command; a present user picks in seconds.
const RESOLVE_PROJECT_TIMEOUT_MS = 5 * 60 * 1000;

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

  // A lexicon code is valid only if the backend resolves it to a project with a vernacular writing
  // system. Used to validate the setting on write and to re-check a stored code before acting on it
  // (a lexicon can be deleted in FW Lite after it was selected).
  //
  // Only 404 (no such lexicon) and 400 (a code that can never resolve) count as "invalid" here —
  // since a wrong answer discards the user's stored choice, every other failure (FW Lite still
  // starting up, a transient backend fault) is treated as "still valid".
  const isLexiconCodeValid = async (lexiconCode: string): Promise<boolean> => {
    try {
      return (await fwLiteApi.getWritingSystems(lexiconCode)).vernacular.length > 0;
    } catch (e) {
      if (e instanceof HttpStatusError && (e.status === 400 || e.status === 404)) return false;
      logger.warn(`Could not verify lexicon '${lexiconCode}':`, getErrorMessage(e));
      return true;
    }
  };

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
      return isLexiconCodeValid(newValue);
    },
  );

  /* Manage project info and WebViews */

  const projectManagers = new ProjectManagers(isLexiconCodeValid);

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

      let url: string;
      try {
        url = await fwLiteApi.getBrowseUrl(lexiconCode);
      } catch (e) {
        logger.error(
          `Error resolving browse URL for lexicon '${lexiconCode}':`,
          getErrorMessage(e),
        );
        return { success };
      }
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

      const lexiconCode = await projectManager.getLexiconCodeOrOpenSelector();
      if (!lexiconCode) return { success };

      logger.info(`Displaying entry '${entryId}' in lexicon '${lexiconCode}'`);
      let url: string;
      try {
        url = await fwLiteApi.getBrowseUrl(lexiconCode, entryId);
      } catch (e) {
        logger.error(
          `Error resolving browse URL for lexicon '${lexiconCode}':`,
          getErrorMessage(e),
        );
        return { success };
      }
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

  // Store the lexicon choice on the project and cache its analysis language. setLexiconCode runs the
  // registered validator (a writing-systems check), so it throws if the code doesn't resolve — which
  // is how download-and-select refuses to record a project whose download didn't really land.
  const applyLexiconSelection = async (
    projectManager: ProjectManager,
    lexiconCode: string,
  ): Promise<void> => {
    await projectManager.setLexiconCode(lexiconCode);
    // An empty code is a valid "clear" — it still gets persisted above, but there's no lexicon left
    // to look up an analysis language for.
    if (!lexiconCode) return;
    // Best-effort: the code was already validated by setLexiconCode, so a failure here is transient;
    // fall back to no analysis language rather than failing the (already-stored) selection.
    const langs = await fwLiteApi
      .getWritingSystems(lexiconCode)
      .catch((e) => logger.error('Error fetching writing systems:', getErrorMessage(e)));
    const analysisLang = langs?.analysis[0]?.wsId ?? '';
    if (analysisLang) {
      logger.info(`Storing lexicon analysis language '${analysisLang}'`);
    } else {
      logger.info('Failed to get analysis language of the lexicon');
    }
    await projectManager
      .setAnalysisLanguage(analysisLang)
      .catch((e) => logger.error('Error setting analysis language:', getErrorMessage(e)));
  };

  // Resolving may open the core project picker and wait on the user, so it lives in its own
  // command with a generous timeout instead of eating into the timeout of the command that acts.
  const resolveProjectCommandPromise = papi.commands.registerCommand(
    'lexicon.resolveProject',
    async (webViewId: string) => {
      const projectManager =
        await projectManagers.getProjectManagerFromWebViewIdOrSelectProject(webViewId);
      return { projectId: projectManager?.projectId, projectName: await projectManager?.getName() };
    },
    undefined,
    { timeoutMilliseconds: RESOLVE_PROJECT_TIMEOUT_MS },
  );

  const selectLexiconCommandPromise = papi.commands.registerCommand(
    'lexicon.selectLexicon',
    async (projectId: string, lexiconCode: string) => {
      logger.info(`Selecting lexicon '${lexiconCode}' for project '${projectId}'`);
      const projectManager = projectManagers.getProjectManagerFromProjectId(projectId);
      if (!projectManager) return { success: false };

      await applyLexiconSelection(projectManager, lexiconCode);
      return { success: true };
    },
  );

  const remoteProjectsCommandPromise = papi.commands.registerCommand(
    'lexicon.remoteProjects',
    async () => {
      try {
        const [remote, local] = await Promise.all([
          fwLiteApi.getRemoteProjects(),
          fwLiteApi.getProjects(),
        ]);
        // Dedupe against ALL local projects — not the language-filtered list the web view holds —
        // so an already-downloaded project can't reappear as downloadable.
        return remote.filter(
          (r) => !local.some((l) => l.id && l.id === r.id && l.server?.id === r.server?.id),
        );
      } catch (e) {
        logger.error('Error fetching remote projects:', getErrorMessage(e));
        return undefined;
      }
    },
  );

  const downloadAndSelectLexiconCommandPromise = papi.commands.registerCommand(
    'lexicon.downloadAndSelectLexicon',
    async (projectId: string, authority: string, lexiconCode: string) => {
      logger.info(`Downloading '${lexiconCode}' from '${authority}' for project '${projectId}'`);
      const projectManager = projectManagers.getProjectManagerFromProjectId(projectId);
      if (!projectManager) return { result: 'Error' as const, success: false };

      // Abort the backend download once the command times out, so an abandoned wait doesn't linger.
      const abort = new AbortController();
      const timeout = setTimeout(() => abort.abort(), DOWNLOAD_TIMEOUT_MS);
      let result: DownloadResult;
      try {
        result = await fwLiteApi.downloadProject(authority, lexiconCode, abort.signal);
      } catch (e) {
        logger.error('Error downloading project:', getErrorMessage(e));
        return { result: 'Error' as const, success: false };
      } finally {
        clearTimeout(timeout);
      }
      // AlreadyDownloaded is fine — the project is local, so go ahead and select it.
      if (result !== 'Success' && result !== 'AlreadyDownloaded') return { result, success: false };

      try {
        await applyLexiconSelection(projectManager, lexiconCode);
        return { result, success: true };
      } catch (e) {
        // Downloaded, but selection failed (e.g. the validator's writing-systems check). Report the
        // download result but success:false so the web view surfaces a failure and keeps the picker.
        logger.error('Downloaded but failed to select lexicon:', getErrorMessage(e));
        return { result, success: false };
      }
    },
    undefined,
    { timeoutMilliseconds: DOWNLOAD_TIMEOUT_MS },
  );

  // DEV-ONLY: a quick lexicon switcher. Lexicon selection is intentionally sticky — once a project
  // has one, the only supported way to change it is clearing `lexicon.lexiconCode` in the project
  // settings (the next lexicon action then reopens the selector). This menu command is a
  // development convenience to be removed before release, along with:
  //   - its entry in the `context.registrations.add(...)` list below,
  //   - the `lexicon.changeLexicon` handler type in `src/types/lexicon.d.ts`,
  //   - the `%lexicon_menu_selectLexicon%` menu item in `contributions/menus.json`, and
  //   - the `%lexicon_menu_selectLexicon%` string in `contributions/localizedStrings.json`.
  // (`ProjectManager.openSelector` stays — the non-dev clear-and-reopen path uses it too.)
  const changeLexiconCommandPromise = papi.commands.registerCommand(
    'lexicon.changeLexicon',
    async (webViewId: string) => {
      const projectManager =
        await projectManagers.getProjectManagerFromWebViewIdOrSelectProject(webViewId);
      if (!projectManager) return { success: false };
      const success = await projectManager.openSelector();
      return { success };
    },
  );

  const deleteDownloadedLexiconCommandPromise = papi.commands.registerCommand(
    'lexicon.deleteDownloadedLexicon',
    async (lexiconCode: string) => {
      try {
        // A CRDT lexicon can be deleted here, downloaded or local-only; FwData projects are managed
        // by FieldWorks, so refuse those.
        const project = (await fwLiteApi.getProjects()).find((p) => p.code === lexiconCode);
        if (!project?.crdt) {
          return { success: false, error: `Lexicon '${lexiconCode}' can't be deleted here` };
        }
        logger.info(`Deleting lexicon '${lexiconCode}'`);
        await fwLiteApi.deleteProject(lexiconCode);
        return { success: true };
      } catch (e) {
        const error = getErrorMessage(e);
        logger.error('Error deleting downloaded lexicon:', error);
        return { success: false, error };
      }
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
    async (projectId?: string, all?: boolean, keepCodes?: string[]) => {
      logger.info('Fetching local lexicons');
      if (!projectId || all)
        return { projects: await fwLiteApi.getProjects(), filtered: false, noMatch: false };

      const projectManager = projectManagers.getProjectManagerFromProjectId(projectId);
      // A stale projectId (e.g. from a restored layout) must not take down the whole list;
      // it only costs the language-based filtering.
      const langTag = await projectManager?.getLanguageTag().catch((e) => {
        logger.warn(`Could not get language tag for project '${projectId}':`, getErrorMessage(e));
        return undefined;
      });
      // Keep the current lexicon plus any applied earlier this session (keepCodes) even when their
      // language doesn't match, so what's in use — and what was just replaced — stays visible.
      const currentCode = await projectManager?.getLexiconCode().catch((e) => {
        logger.warn(
          `Could not get current lexicon for project '${projectId}':`,
          getErrorMessage(e),
        );
        return undefined;
      });
      const keep = [...new Set([currentCode, ...(keepCodes ?? [])])].filter(
        (c): c is string => !!c,
      );
      const result = await fwLiteApi.getProjectsMatchingLanguage(langTag, keep);
      // The language label is needed for both the "filtered to X" bar and the "nothing matched X" note.
      return { ...result, langTag: result.filtered || result.noMatch ? langTag : undefined };
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
    await changeLexiconCommandPromise, // DEV-ONLY: remove before release (see registration above)
    await createLexiconCommandPromise,
    await deleteDownloadedLexiconCommandPromise,
    await displayEntryCommandPromise,
    await findEntryCommandPromise,
    await findRelatedEntriesCommandPromise,
    await lexiconsCommandPromise,
    await loginCommandPromise,
    await logoutCommandPromise,
    await remoteProjectsCommandPromise,
    await resolveProjectCommandPromise,
    await downloadAndSelectLexiconCommandPromise,
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
 * Per-user directory for FW Lite data (projects, auth cache), separate from Platform.Bible's own
 * `papi.storage` data for this extension. Mirrors paranext-core's `app://`/`getAppDir()` scheme:
 * the real per-user location when packaged, repo-local dev-appdata in development, so `npm start`
 * doesn't touch production user data.
 *
 * Builds paths by hand (no require('os'/'path') — Platform.Bible blocks non-papi requires), using
 * the platform separator: backslash on Windows, forward slash elsewhere (which .NET requires).
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
