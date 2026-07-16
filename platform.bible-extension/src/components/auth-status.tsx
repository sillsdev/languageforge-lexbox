import { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import { Button } from 'platform-bible-react';
import { type ReactElement, useState } from 'react';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';
import type { AuthServerStatus, LoginResult } from '../utils/fw-lite-api';

/** Props for the AuthStatus component */
interface AuthStatusProps {
  servers?: AuthServerStatus[];
  login: (authority: string) => Promise<LoginResult | undefined>;
  logout: (authority: string) => Promise<void>;
}

/** Shows each configured Lexbox server's sign-in status with a Login/Logout button. */
export default function AuthStatus({
  servers,
  login,
  logout,
}: AuthStatusProps): ReactElement | undefined {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  const [pendingAuthorities, setPendingAuthorities] = useState(new Set<string>());
  const [signInErrors, setSignInErrors] = useState(new Map<string, string>());

  function setPending(authority: string, pending: boolean): void {
    setPendingAuthorities((prev) => {
      const next = new Set(prev);
      if (pending) next.add(authority);
      else next.delete(authority);
      return next;
    });
  }

  function setSignInError(authority: string, message?: string): void {
    setSignInErrors((prev) => {
      const next = new Map(prev);
      if (message) next.set(authority, message);
      else next.delete(authority);
      return next;
    });
  }

  function handleLogin(authority: string): void {
    setSignInError(authority, undefined);
    setPending(authority, true);
    // eslint-disable-next-line promise/catch-or-return
    login(authority)
      .then((result) => {
        // Cancelled was the user's own choice; anything else short of Success needs surfacing.
        if (result !== 'Success' && result !== 'Cancelled')
          setSignInError(
            authority,
            result === 'Offline'
              ? localizedStrings['%lexicon_auth_signInOffline%']
              : localizedStrings['%lexicon_auth_signInFailed%'],
          );
        return undefined;
      })
      .catch((e) => {
        logger.error(localizedStrings['%lexicon_auth_loginError%'], JSON.stringify(e));
        setSignInError(authority, localizedStrings['%lexicon_auth_signInFailed%']);
      })
      .finally(() => setPending(authority, false));
  }

  function handleLogout(authority: string): void {
    setSignInError(authority, undefined);
    setPending(authority, true);
    // eslint-disable-next-line promise/catch-or-return
    logout(authority)
      .catch((e) => logger.error(localizedStrings['%lexicon_auth_logoutError%'], JSON.stringify(e)))
      .finally(() => setPending(authority, false));
  }

  if (!servers?.length) return undefined;

  return (
    <div className="tw:flex tw:flex-col tw:gap-2 tw:p-4 tw:border-b">
      <h3 className="tw:font-semibold">{localizedStrings['%lexicon_auth_sectionTitle%']}</h3>
      {servers.map((status) => {
        const isPending = pendingAuthorities.has(status.server.id);
        const signInError = status.loggedIn ? undefined : signInErrors.get(status.server.id);
        return (
          <div
            className="tw:flex tw:items-center tw:justify-between tw:gap-2"
            key={status.server.id}
          >
            <div>
              <div>{status.displayName}</div>
              <div className="tw:text-sm tw:text-muted-foreground">
                {status.loggedIn
                  ? `${localizedStrings['%lexicon_auth_signedInAs%']} ${status.loggedInAs}`
                  : localizedStrings['%lexicon_auth_signedOut%']}
              </div>
              {signInError && (
                <div className="tw:text-sm tw:text-destructive" role="alert">
                  {signInError}
                </div>
              )}
            </div>

            {status.loggedIn ? (
              <Button
                disabled={isPending}
                onClick={() => handleLogout(status.server.id)}
                type="button"
                variant="secondary"
              >
                {localizedStrings['%lexicon_auth_logout%']}
              </Button>
            ) : (
              <Button
                disabled={isPending}
                onClick={() => handleLogin(status.server.id)}
                type="button"
              >
                {isPending
                  ? localizedStrings['%lexicon_auth_loggingIn%']
                  : localizedStrings['%lexicon_auth_login%']}
              </Button>
            )}
          </div>
        );
      })}
    </div>
  );
}
