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

  // Per-authority so concurrent sign-ins on different servers disable their rows independently.
  const [pendingAuthorities, setPendingAuthorities] = useState(() => new Set<string>());
  const [signInErrors, setSignInErrors] = useState(() => new Map<string, string>());

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
      <div className="tw:flex tw:flex-col tw:gap-4">
        {servers.map((status) => {
          const isPending = pendingAuthorities.has(status.server.id);
          const signInError = status.loggedIn ? undefined : signInErrors.get(status.server.id);
          return (
            <div className="tw:flex tw:flex-col tw:gap-1" key={status.server.id}>
              {servers.length > 1 && (
                <div className="tw:text-sm tw:font-medium" title={status.displayName}>
                  {status.displayName}
                </div>
              )}

              {status.loggedIn ? (
                <div className="tw:flex tw:items-center tw:gap-3">
                  <span className="tw:min-w-0 tw:truncate tw:text-sm tw:text-muted-foreground">
                    {localizedStrings['%lexicon_auth_signedInAs%']}{' '}
                    <span className="tw:text-foreground tw:font-medium" title={status.loggedInAs}>
                      {status.loggedInAs}
                    </span>
                  </span>
                  <Button
                    className="tw:shrink-0"
                    disabled={isPending}
                    onClick={() => handleLogout(status.server.id)}
                    size="sm"
                    type="button"
                    variant="secondary"
                  >
                    {localizedStrings['%lexicon_auth_logout%']}
                  </Button>
                </div>
              ) : (
                <Button
                  className="tw:self-start"
                  disabled={isPending}
                  onClick={() => handleLogin(status.server.id)}
                  type="button"
                >
                  {isPending
                    ? localizedStrings['%lexicon_auth_loggingIn%']
                    : localizedStrings['%lexicon_auth_login%']}
                </Button>
              )}

              {signInError && (
                <div className="tw:text-sm tw:text-destructive" role="alert">
                  {signInError}
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}
