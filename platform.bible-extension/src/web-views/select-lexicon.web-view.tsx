import type { WebViewProps } from '@papi/core';
import { commands, logger } from '@papi/frontend';
import type { IProjectModel } from 'lexicon';
import { useCallback, useEffect, useState } from 'react';
import AuthStatus from '../components/auth-status';
import LexiconComboBox from '../components/lexicon-combo-box';
import type { AuthServerStatus, LoginResult } from '../utils/fw-lite-api';

globalThis.webViewComponent = function LexiconSelect({ projectId }: WebViewProps) {
  const [lexicons, setLexicons] = useState<IProjectModel[] | undefined>();
  const [authServers, setAuthServers] = useState<AuthServerStatus[] | undefined>();

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
      await commands.sendCommand('lexicon.selectLexicon', projectId ?? '', code);
    },
    [projectId],
  );

  useEffect(() => {
    logger.info(`This WebView was opened for project '${projectId}'`);
    commands
      .sendCommand('lexicon.lexicons', projectId)
      .then(setLexicons)
      .catch((e) => logger.error('Error fetching lexicons:', JSON.stringify(e)));
  }, [projectId]);

  useEffect(() => {
    refreshAuthServers();
  }, [refreshAuthServers]);

  return (
    <>
      <AuthStatus login={login} logout={logout} servers={authServers} />
      <LexiconComboBox lexicons={lexicons} selectLexicon={selectLexicon} />
    </>
  );
};
