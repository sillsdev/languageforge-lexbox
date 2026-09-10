import { commands, logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import type { IProjectModel, LexiconWebViewProps } from 'lexicon';
import { useCallback, useEffect, useState } from 'react';
import AuthStatus from '../components/auth-status';
import CreateLexicon from '../components/create-lexicon';
import LexiconComboBox from '../components/lexicon-combo-box';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';
import type { AuthServerStatus, LoginResult } from '../utils/fw-lite-api';

globalThis.webViewComponent = function LexiconSelect({
  projectId,
  vernacularLanguage,
}: LexiconWebViewProps) {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);
  const [authServers, setAuthServers] = useState<AuthServerStatus[] | undefined>();
  const [done, setDone] = useState(false);
  const [lexicons, setLexicons] = useState<IProjectModel[] | undefined>();
  const [showCreate, setShowCreate] = useState(false);

  const fetchLexicons = useCallback(() => {
    commands
      .sendCommand('lexicon.lexicons', projectId)
      .then(setLexicons)
      .catch((e) => logger.error('Error fetching lexicons:', JSON.stringify(e)));
  }, [projectId]);

  useEffect(() => {
    logger.info(`This WebView was opened for project '${projectId}'`);
    fetchLexicons();
  }, [fetchLexicons, projectId]);

  // Keeps the last-known list when a refresh returns nothing, so the section doesn't vanish.
  const applyServers = useCallback(
    (next?: AuthServerStatus[]) => setAuthServers((prev) => next ?? prev),
    [],
  );

  const refreshAuthServers = useCallback(() => {
    commands
      .sendCommand('lexicon.authServers')
      .then(applyServers)
      .catch((e) => logger.error('Error fetching Lexbox auth servers:', JSON.stringify(e)));
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
      }
    },
    [applyServers, refreshAuthServers],
  );

  const logout = useCallback(
    async (authority: string): Promise<void> => {
      try {
        applyServers(await commands.sendCommand('lexicon.logout', authority));
      } catch (e) {
        // Sign-out may have failed server-side; re-fetch so the row reflects the real status.
        refreshAuthServers();
        throw e;
      }
    },
    [applyServers, refreshAuthServers],
  );

  const selectLexicon = useCallback(
    async (code: string): Promise<void> => {
      const result = await commands.sendCommand('lexicon.selectLexicon', projectId ?? '', code);
      if (!result?.success) throw new Error(result?.error || 'Failed to select lexicon');
    },
    [projectId],
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
        await selectLexicon(code);
        setDone(true);
      } catch (e) {
        // Lexicon created but auto-selection failed; return to the project-selection combo box.
        logger.error('Error auto-selecting created lexicon:', JSON.stringify(e));
        fetchLexicons();
        setShowCreate(false);
      }
    },
    [fetchLexicons, selectLexicon],
  );

  if (done) {
    return (
      <h3 className="tw:font-semibold tw:m-2">
        {localizedStrings['%lexicon_selectLexicon_saved%']}
      </h3>
    );
  }

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
    <>
      <AuthStatus login={login} logout={logout} servers={authServers} />
      <LexiconComboBox
        lexicons={lexicons}
        onCreateNew={() => setShowCreate(true)}
        selectLexicon={selectLexicon}
      />
    </>
  );
};
