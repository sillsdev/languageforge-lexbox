import { commands, logger } from '@papi/frontend';
import type { IProjectModel, LexiconWebViewProps } from 'lexicon';
import { getErrorMessage } from 'platform-bible-utils';
import { useCallback, useEffect, useRef, useState } from 'react';
import AuthStatus from '../components/auth-status';
import CreateLexicon from '../components/create-lexicon';
import LexiconPicker from '../components/lexicon-picker';
import type { AuthServerStatus, DownloadResult, LoginResult } from '../utils/fw-lite-api';

globalThis.webViewComponent = function LexiconSelect({
  id: webViewId,
  projectId,
  lexiconCode,
  vernacularLanguage,
  projectName,
}: LexiconWebViewProps) {
  const [authServers, setAuthServers] = useState<AuthServerStatus[] | undefined>();
  // Name of the lexicon whose save just landed; drives the picker's one-time confirmation line.
  const [savedName, setSavedName] = useState<string | undefined>();
  // Lexicons applied during this panel session, kept in the list past the language filter so a
  // just-replaced non-matching lexicon doesn't vanish. Seeded with the lexicon current at open.
  const sessionKeptCodes = useRef(new Set<string>(lexiconCode ? [lexiconCode] : []));
  // The lexicon currently applied to the project, so the picker pre-selects it (starts from the
  // prop, then tracks each save). The short name of the resolved project, for labelling.
  const [currentCode, setCurrentCode] = useState(lexiconCode);
  const [currentProjectName, setCurrentProjectName] = useState(projectName);
  const [lexicons, setLexicons] = useState<IProjectModel[] | undefined>();
  const [remoteProjects, setRemoteProjects] = useState<IProjectModel[] | undefined>();
  const [showCreate, setShowCreate] = useState(false);

  // When the language filter kicked in, the user can flip to the full list (per-panel choice).
  const [showAll, setShowAll] = useState(false);
  const [languageFiltered, setLanguageFiltered] = useState(false);
  // A language was known but no local lexicon matched it, so the full list is shown with a note.
  const [languageNoMatch, setLanguageNoMatch] = useState(false);
  const [filterLangTag, setFilterLangTag] = useState<string | undefined>();

  const fetchLexicons = useCallback(() => {
    commands
      .sendCommand('lexicon.lexicons', projectId, showAll, [...sessionKeptCodes.current])
      .then((result) => {
        setLexicons(result?.projects);
        setLanguageFiltered(!!result?.filtered);
        setLanguageNoMatch(!!result?.noMatch);
        setFilterLangTag(result?.langTag);
        return undefined;
      })
      .catch((e) => logger.error('Error fetching lexicons:', getErrorMessage(e)));
  }, [projectId, showAll]);

  const fetchRemoteProjects = useCallback(() => {
    commands
      .sendCommand('lexicon.remoteProjects')
      .then(setRemoteProjects)
      .catch((e) => logger.error('Error fetching remote projects:', getErrorMessage(e)));
  }, []);

  // A save just landed: remember the lexicon for the confirmation line, track it as current so the
  // picker shows it checked, keep it visible for the rest of the session, and refresh the lists (a
  // just-downloaded remote now appears as local).
  const handleSaved = useCallback(
    (name: string, code: string) => {
      sessionKeptCodes.current.add(code);
      setCurrentCode(code);
      setSavedName(name);
      fetchLexicons();
      fetchRemoteProjects();
    },
    [fetchLexicons, fetchRemoteProjects],
  );

  useEffect(() => {
    logger.info(`This WebView was opened for project '${projectId}'`);
    fetchLexicons();
    fetchRemoteProjects();
  }, [fetchLexicons, fetchRemoteProjects, projectId]);

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
        // Now that a server is signed in, its projects can be listed.
        fetchRemoteProjects();
        return result;
      } catch (e) {
        // A sign-in can land even after the command fails (e.g. PAPI request timeout).
        refreshAuthServers();
        fetchRemoteProjects();
        throw e;
      }
    },
    [applyServers, fetchRemoteProjects, refreshAuthServers],
  );

  const logout = useCallback(
    async (authority: string): Promise<void> => {
      try {
        applyServers(await commands.sendCommand('lexicon.logout', authority));
        // Its remote projects are no longer accessible; drop them from the list.
        fetchRemoteProjects();
      } catch (e) {
        // Sign-out may have failed server-side; re-fetch so the row reflects the real status.
        refreshAuthServers();
        fetchRemoteProjects();
        throw e;
      }
    },
    [applyServers, fetchRemoteProjects, refreshAuthServers],
  );

  // The projectId prop isn't trusted for actions: it can be missing or stale (a tab restored from a
  // saved layout after the project went away). On the first action, lexicon.resolveProject verifies
  // the web view's project — prompting the user with the core project picker if needed — and the
  // result is remembered so later actions in this panel don't re-resolve. undefined = the user
  // dismissed the prompt.
  const [resolvedProjectId, setResolvedProjectId] = useState<string | undefined>();
  const resolveProjectId = useCallback(async (): Promise<string | undefined> => {
    if (resolvedProjectId) return resolvedProjectId;
    const { projectId: picked, projectName: pickedName } = await commands.sendCommand(
      'lexicon.resolveProject',
      webViewId ?? '',
    );
    if (picked) setResolvedProjectId(picked);
    // A tab restored without a project only learns its name here; keep the label in sync.
    if (pickedName) setCurrentProjectName(pickedName);
    return picked;
  }, [resolvedProjectId, webViewId]);

  const selectLexicon = useCallback(
    async (code: string): Promise<{ cancelled?: boolean }> => {
      const targetProjectId = await resolveProjectId();
      if (!targetProjectId) return { cancelled: true };
      const result = await commands.sendCommand('lexicon.selectLexicon', targetProjectId, code);
      if (!result?.success) throw new Error(result?.error || 'Failed to select lexicon');
      return {};
    },
    [resolveProjectId],
  );

  const downloadAndSelect = useCallback(
    async (
      authority: string,
      code: string,
    ): Promise<{ result: DownloadResult; success: boolean; cancelled?: boolean }> => {
      const targetProjectId = await resolveProjectId();
      if (!targetProjectId) return { result: 'Error', success: false, cancelled: true };
      return commands.sendCommand(
        'lexicon.downloadAndSelectLexicon',
        targetProjectId,
        authority,
        code,
      );
    },
    [resolveProjectId],
  );

  const deleteLexicon = useCallback(
    async (code: string): Promise<void> => {
      const result = await commands.sendCommand('lexicon.deleteDownloadedLexicon', code);
      if (!result?.success) throw new Error(result?.error || 'Failed to delete the local copy');
      // The deleted project is now downloadable again; refresh both lists.
      fetchLexicons();
      fetchRemoteProjects();
    },
    [fetchLexicons, fetchRemoteProjects],
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
    async (code: string): Promise<void> => {
      try {
        const { cancelled } = await selectLexicon(code);
        if (!cancelled) {
          // handleSaved refreshes the lists; drop the create screen so the picker (with its success
          // banner) shows.
          handleSaved(code, code);
          setShowCreate(false);
          return;
        }
      } catch (e) {
        logger.error('Error auto-selecting created lexicon:', getErrorMessage(e));
      }
      // Created but not selected (failed, or the user dismissed the project prompt); back to the picker.
      fetchLexicons();
      setShowCreate(false);
    },
    [fetchLexicons, handleSaved, selectLexicon],
  );

  if (showCreate) {
    return (
      <CreateLexicon
        createLexicon={createLexicon}
        defaultVernacularWs={vernacularLanguage}
        existingCodes={lexicons?.map((l) => l.code)}
        onCancel={() => setShowCreate(false)}
        onCreated={onCreated}
      />
    );
  }

  return (
    // Only the picker's list scrolls; the outer scrollbar exists solely as a fallback for windows
    // too short to fit the fixed chrome at all. The width cap keeps a docked full-width tab from
    // producing absurdly wide rows while still letting a widened float un-truncate long codes.
    <div className="tw:flex tw:flex-col tw:h-screen tw:overflow-y-auto">
      <div className="tw:flex tw:flex-col tw:flex-1 tw:min-h-0 tw:w-full tw:max-w-3xl tw:mx-auto">
        <div className="tw:shrink-0">
          <AuthStatus login={login} logout={logout} servers={authServers} />
        </div>
        <LexiconPicker
          loading={!lexicons}
          localProjects={lexicons}
          remoteProjects={remoteProjects}
          signedIn={!!authServers?.some((s) => s.loggedIn)}
          initialCode={currentCode}
          projectName={currentProjectName}
          savedName={savedName}
          onClearSaved={() => setSavedName(undefined)}
          languageFiltered={languageFiltered}
          languageNoMatch={languageNoMatch}
          filterLangTag={filterLangTag}
          onShowAll={() => setShowAll(true)}
          onCreateNew={() => setShowCreate(true)}
          selectLexicon={selectLexicon}
          downloadAndSelect={downloadAndSelect}
          deleteLexicon={deleteLexicon}
          onSaved={handleSaved}
        />
      </div>
    </div>
  );
};
