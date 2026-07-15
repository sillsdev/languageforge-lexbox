import { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import { Button } from 'platform-bible-react';
import { type ReactElement, useEffect, useState } from 'react';
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

  // Tracks which servers have a login/logout call in flight so each row's button disables
  // independently — a slow sign-in on one server must not re-enable another's button (or clear its
  // pending state), since multiple can be pending at once.
  const [pendingAuthorities, setPendingAuthorities] = useState<Set<string>>(() => new Set());

  function setPending(authority: string, pending: boolean): void {
    setPendingAuthorities((prev) => {
      const next = new Set(prev);
      if (pending) next.add(authority);
      else next.delete(authority);
      return next;
    });
  }

  // A user-facing message per server whose last sign-in didn't complete. Sign-in fails silently
  // otherwise — the blocking login call just resolves and the button un-presses — which looks
  // indistinguishable from success (e.g. offline, or the browser flow never finished).
  const [signInErrors, setSignInErrors] = useState<Map<string, string>>(() => new Map());

  function setSignInError(authority: string, message?: string): void {
    setSignInErrors((prev) => {
      const next = new Map(prev);
      if (message) next.set(authority, message);
      else next.delete(authority);
      return next;
    });
  }

  // Drop a server's stale sign-in error once it reports signed-in. The sign-in may complete through
  // a path this component never sees (the embedded viewer, another webview, or the browser flow
  // landing after login()'s command threw), so relying on the Login/Logout handlers alone would
  // leave the old message in the map to resurface if the row later returns to signed-out.
  useEffect(() => {
    const loggedInIds = (servers ?? []).filter((s) => s.loggedIn).map((s) => s.server.id);
    setSignInErrors((prev) => {
      if (!loggedInIds.some((id) => prev.has(id))) return prev;
      const next = new Map(prev);
      loggedInIds.forEach((id) => next.delete(id));
      return next;
    });
  }, [servers]);

  function handleLogin(authority: string): void {
    setSignInError(authority, undefined);
    setPending(authority, true);
    // eslint-disable-next-line promise/catch-or-return
    login(authority)
      .then((result) => {
        // Success flips the row to signed-in; Cancelled is user-initiated, so stay silent.
        // Everything else (Offline, or a hard failure the command swallowed to undefined) left no
        // trace before — surface it so the user knows the click didn't sign them in.
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
        const signInError = !status.loggedIn ? signInErrors.get(status.server.id) : undefined;
        return (
          <div className="tw:flex tw:flex-col tw:gap-1" key={status.server.id}>
            <div className="tw:flex tw:items-center tw:justify-between tw:gap-2">
              <div>
                <div>{status.displayName}</div>
                <div className="tw:text-sm tw:text-muted-foreground">
                  {status.loggedIn
                    ? `${localizedStrings['%lexicon_auth_signedInAs%']} ${status.loggedInAs}`
                    : localizedStrings['%lexicon_auth_signedOut%']}
                </div>
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

            {signInError && (
              <div className="tw:text-sm tw:text-destructive" role="alert">
                {signInError}
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}
