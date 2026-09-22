import { commands, localization, logger } from '@papi/frontend';
import type { IProjectModel, LexiconWebViewProps } from 'lexicon';
import { Alert, AlertDescription } from 'platform-bible-react';
import { formatReplacementString, getErrorMessage } from 'platform-bible-utils';
import { useCallback, useEffect, useRef, useState } from 'react';
import AuthStatus from '../components/auth-status';
import CreateLexicon from '../components/create-lexicon';
import LexiconPicker from '../components/lexicon-picker';
import type {
  AuthServerStatus,
  DownloadAndSelectResult,
  LocalLexiconsResult,
  LoginResult,
} from '../utils/fw-lite-api';

async function titleForProject(projectName: string): Promise<string> {
  const template = await localization.getLocalizedString({
    localizeKey: '%lexicon_webViewTitle_selectLexiconForProject%',
  });
  return formatReplacementString(template, { project: projectName });
}

// The backend resolves the project from the web view id, so nothing here reads the open-time props.
globalThis.webViewComponent = function LexiconSelect({
  id: webViewId,
  updateWebViewDefinition,
}: LexiconWebViewProps) {
  const [authServers, setAuthServers] = useState<AuthServerStatus[] | undefined>();
  const [lexiconList, setLexiconList] = useState<LocalLexiconsResult | undefined>();
  const [listError, setListError] = useState('');
  const [remoteProjects, setRemoteProjects] = useState<IProjectModel[] | undefined>();
  const [savedName, setSavedName] = useState<string | undefined>();
  const [showAll, setShowAll] = useState(false);
  const [showCreate, setShowCreate] = useState(false);
  const [downloading, setDownloading] = useState(false);
  // Lexicons applied this session stay listed even when the language filter would hide them, so a
  // just-replaced non-matching lexicon doesn't vanish.
  const sessionKeptCodes = useRef(new Set<string>());

  // The language-filtered query is slower, so a late response could undo a newer unfiltered list.
  const fetchSeq = useRef(0);

  const fetchLexicons = useCallback(() => {
    fetchSeq.current += 1;
    const seq = fetchSeq.current;
    setListError('');
    commands
      .sendCommand('lexicon.lexicons', webViewId, showAll, [...sessionKeptCodes.current])
      .then((result) => {
        if (seq === fetchSeq.current) setLexiconList(result);
        return undefined;
      })
      .catch((e) => {
        const message = getErrorMessage(e);
        logger.error('Error fetching lexicons:', message);
        if (seq === fetchSeq.current) setListError(message);
      });
  }, [webViewId, showAll]);

  useEffect(() => {
    fetchLexicons();
  }, [fetchLexicons]);

  // undefined means the fetch itself failed, so keep the last-known list rather than dropping every
  // server group. Signing out is not that case: it returns an empty list, which correctly clears them.
  const fetchRemoteProjects = useCallback(() => {
    commands
      .sendCommand('lexicon.remoteProjects')
      .then((next?: IProjectModel[]) => setRemoteProjects((prev) => next ?? prev))
      .catch((e) => logger.error('Error fetching remote projects:', getErrorMessage(e)));
  }, []);

  useEffect(() => {
    fetchRemoteProjects();
  }, [fetchRemoteProjects]);

  const handleSaved = useCallback(
    (name: string, code: string) => {
      sessionKeptCodes.current.add(code);
      setSavedName(name);
      fetchLexicons();
      fetchRemoteProjects();
    },
    [fetchLexicons, fetchRemoteProjects],
  );

  // Keeps the last-known list when a refresh returns nothing, so the section doesn't vanish.
  const applyServers = useCallback(
    (next?: AuthServerStatus[]) => setAuthServers((prev) => next ?? prev),
    [],
  );

  const refreshAuthServers = useCallback(() => {
    commands
      .sendCommand('lexicon.authServers')
      .then(applyServers)
      .catch((e) => logger.error('Error fetching Lexbox auth servers:', getErrorMessage(e)));
  }, [applyServers]);

  useEffect(() => {
    refreshAuthServers();
  }, [refreshAuthServers]);

  const login = useCallback(
    async (authority: string): Promise<LoginResult | undefined> => {
      try {
        const { result, servers } = await commands.sendCommand('lexicon.login', authority);
        applyServers(servers);
        return result;
      } catch (e) {
        // A sign-in can land even after the command fails (e.g. PAPI request timeout).
        refreshAuthServers();
        throw e;
      } finally {
        fetchRemoteProjects();
      }
    },
    [applyServers, fetchRemoteProjects, refreshAuthServers],
  );

  const logout = useCallback(
    async (authority: string): Promise<void> => {
      try {
        applyServers(await commands.sendCommand('lexicon.logout', authority));
      } catch (e) {
        // Sign-out may have failed server-side; re-fetch so the row reflects the real status.
        refreshAuthServers();
        throw e;
      } finally {
        fetchRemoteProjects();
      }
    },
    [applyServers, fetchRemoteProjects, refreshAuthServers],
  );

  // Writes need a bound project. Prompting goes through lexicon.resolveProject (its own generous
  // timeout) and the answer is stored on the definition, so later reads resolve the same project
  // without prompting again. The id is returned rather than read back off the definition, because
  // that update reaches the extension host asynchronously and the write would race it. Undefined
  // means the user dismissed the prompt.
  const ensureProject = useCallback(async (): Promise<string | undefined> => {
    if (lexiconList?.project) return lexiconList.project.id;
    const { projectId, projectName } = await commands.sendCommand(
      'lexicon.resolveProject',
      webViewId,
    );
    if (!projectId) return undefined;
    const definition = projectName
      ? { projectId, title: await titleForProject(projectName) }
      : { projectId };
    if (!updateWebViewDefinition(definition)) {
      logger.warn(`Could not bind WebView '${webViewId}' to project '${projectId}'`);
    }
    fetchLexicons();
    return projectId;
  }, [lexiconList?.project, webViewId, updateWebViewDefinition, fetchLexicons]);

  const selectLexicon = useCallback(
    async (code: string): Promise<{ cancelled?: boolean }> => {
      const projectId = await ensureProject();
      if (!projectId) return { cancelled: true };
      const result = await commands.sendCommand('lexicon.selectLexicon', projectId, code);
      if (!result?.success) throw new Error(result?.error || 'Failed to select lexicon');
      return {};
    },
    [ensureProject],
  );

  const downloadAndSelect = useCallback(
    async (authority: string, code: string): Promise<DownloadAndSelectResult> => {
      const projectId = await ensureProject();
      if (!projectId) return { result: 'Error', success: false, cancelled: true };
      return commands.sendCommand('lexicon.downloadAndSelectLexicon', projectId, authority, code);
    },
    [ensureProject],
  );

  const deleteLexicon = useCallback(
    async (code: string): Promise<void> => {
      const result = await commands.sendCommand(
        'lexicon.deleteDownloadedLexicon',
        code,
        lexiconList?.project?.id,
      );
      if (!result?.success) throw new Error(result?.error || 'Failed to delete the lexicon');
      fetchLexicons();
      // The deleted lexicon may be downloadable again.
      fetchRemoteProjects();
    },
    [fetchLexicons, fetchRemoteProjects, lexiconList?.project?.id],
  );

  const createLexicon = useCallback(
    async (
      name: string,
      code: string,
      vernacularWs: string,
      analysisWs?: string,
    ): Promise<void> => {
      const result = await commands.sendCommand(
        'lexicon.createLexicon',
        name,
        code,
        vernacularWs,
        analysisWs,
      );
      if (!result?.success) throw new Error(result?.error || 'Failed to create lexicon');
    },
    [],
  );

  const onCreated = useCallback(
    async (name: string, code: string): Promise<void> => {
      let selected = false;
      try {
        selected = !(await selectLexicon(code)).cancelled;
      } catch (e) {
        logger.error('Error auto-selecting created lexicon:', getErrorMessage(e));
      }
      if (selected) handleSaved(name, code);
      else fetchLexicons();
      setShowCreate(false);
    },
    [fetchLexicons, handleSaved, selectLexicon],
  );

  if (showCreate) {
    return (
      <CreateLexicon
        createLexicon={createLexicon}
        defaultVernacularWs={lexiconList?.project?.langTag}
        // Remote codes count too: creating a lexicon whose code matches a remote one would suppress
        // that remote from the download list.
        existingCodes={[
          ...(lexiconList?.projects.map((l) => l.code) ?? []),
          ...(remoteProjects?.map((p) => p.code) ?? []),
        ]}
        onCancel={() => setShowCreate(false)}
        onCreated={onCreated}
      />
    );
  }

  return (
    <div className="tw:flex tw:flex-col tw:h-screen tw:overflow-y-auto">
      <div className="tw:flex tw:flex-col tw:flex-1 tw:min-h-0 tw:w-full tw:max-w-3xl tw:mx-auto">
        <div className="tw:shrink-0">
          <AuthStatus busy={downloading} login={login} logout={logout} servers={authServers} />
        </div>
        {!!listError && (
          <div className="tw:shrink-0 tw:px-4 tw:pt-4">
            <Alert variant="destructive">
              <AlertDescription role="alert">{listError}</AlertDescription>
            </Alert>
          </div>
        )}
        <LexiconPicker
          loading={!lexiconList && !listError}
          localProjects={lexiconList?.projects}
          remoteProjects={remoteProjects}
          signedIn={!!authServers?.some((s) => s.loggedIn)}
          appliedCode={lexiconList?.project?.lexiconCode}
          projectName={lexiconList?.project?.name}
          savedName={savedName}
          onClearSaved={() => setSavedName(undefined)}
          languageFiltered={!!lexiconList?.filtered}
          languageNoMatch={!!lexiconList?.noMatch}
          filterLangTag={lexiconList?.project?.langTag}
          onShowAll={() => setShowAll(true)}
          onCreateNew={() => setShowCreate(true)}
          selectLexicon={selectLexicon}
          downloadAndSelect={downloadAndSelect}
          deleteLexicon={deleteLexicon}
          onSaved={handleSaved}
          onDownloadingChange={setDownloading}
        />
      </div>
    </div>
  );
};
