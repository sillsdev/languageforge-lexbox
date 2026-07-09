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

  // Apply a refreshed server list, but keep the last-known one when the refresh comes back
  // undefined (e.g. a transient localhost fetch failure) so the auth section doesn't vanish after
  // an otherwise-successful login/logout. An empty array still clears it (no servers configured).
  const applyServers = useCallback(
    (next?: AuthServerStatus[]) => setAuthServers((prev) => next ?? prev),
    [],
  );

  const selectLexicon = useCallback(
    async (code: string): Promise<void> => {
      await commands.sendCommand('lexicon.selectLexicon', projectId ?? '', code);
    },
    [projectId],
  );

  // lexicon.login/lexicon.logout only resolve once the sign-in attempt has fully finished. Both
  // return the refreshed server list (login also returns the sign-in outcome) so the UI doesn't
  // need a separate round trip to pick up the new status.
  const login = useCallback(
    async (authority: string): Promise<void> => {
      try {
        const { result, servers } = await commands.sendCommand('lexicon.login', authority);
        applyServers(servers);
        // The command reports Offline/Cancelled as normal outcomes and swallows hard failures
        // (result === undefined). Cancellation is user-initiated, so leave it silent; surface the
        // rest so a sign-in that didn't complete is visible rather than looking like success.
        if (result !== 'Success' && result !== 'Cancelled')
          logger.warn(`Lexbox sign-in did not complete${result ? `: ${result}` : ''}`);
      } catch (e) {
        // Sign-in resolves only when the user finishes in the browser, which can outlive the PAPI
        // request timeout (default 30s); refresh so a sign-in that landed anyway still shows up.
        // Guard the refresh so its own failure can't mask the original login error we re-throw below.
        await commands
          .sendCommand('lexicon.authServers')
          .then(applyServers)
          .catch((refreshError) =>
            logger.error(
              'Error refreshing Lexbox auth servers after login failure:',
              JSON.stringify(refreshError),
            ),
          );
        throw e;
      }
    },
    [applyServers],
  );

  const logout = useCallback(
    async (authority: string): Promise<void> => {
      applyServers(await commands.sendCommand('lexicon.logout', authority));
    },
    [applyServers],
  );

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
