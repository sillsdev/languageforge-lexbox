import type { WebViewProps } from '@papi/core';
import { commands, logger } from '@papi/frontend';
import type { IProjectModel } from 'lexicon';
import { useCallback, useEffect, useState } from 'react';
import AuthStatus from '../components/auth-status';
import LexiconComboBox from '../components/lexicon-combo-box';
import type { AuthServerStatus } from '../utils/fw-lite-api';

globalThis.webViewComponent = function LexiconSelect({ projectId }: WebViewProps) {
  const [lexicons, setLexicons] = useState<IProjectModel[] | undefined>();
  const [authServers, setAuthServers] = useState<AuthServerStatus[] | undefined>();

  const selectLexicon = useCallback(
    async (code: string): Promise<void> => {
      await commands.sendCommand('lexicon.selectLexicon', projectId ?? '', code);
    },
    [projectId],
  );

  // lexicon.login/lexicon.logout only resolve once the sign-in attempt has fully finished, and
  // return the refreshed server list so the UI doesn't need a separate round trip to pick up the
  // new status.
  const login = useCallback(async (authority: string): Promise<void> => {
    try {
      setAuthServers(await commands.sendCommand('lexicon.login', authority));
    } catch (e) {
      // Sign-in resolves only when the user finishes in the browser, which can outlive the PAPI
      // request timeout (default 30s); refresh so a sign-in that landed anyway still shows up.
      setAuthServers(await commands.sendCommand('lexicon.authServers'));
      throw e;
    }
  }, []);

  const logout = useCallback(async (authority: string): Promise<void> => {
    setAuthServers(await commands.sendCommand('lexicon.logout', authority));
  }, []);

  useEffect(() => {
    logger.info(`This WebView was opened for project '${projectId}'`);
    commands
      .sendCommand('lexicon.lexicons', projectId)
      .then(setLexicons)
      .catch((e) => logger.error('Error fetching lexicons:', JSON.stringify(e)));
    commands
      .sendCommand('lexicon.authServers')
      .then(setAuthServers)
      .catch((e) => logger.error('Error fetching Lexbox auth servers:', JSON.stringify(e)));
  }, [projectId]);

  return (
    <>
      <AuthStatus login={login} logout={logout} servers={authServers} />
      <LexiconComboBox lexicons={lexicons} selectLexicon={selectLexicon} />
    </>
  );
};
