import { logger } from '@papi/frontend';
import { useLocalizedStrings } from '@papi/frontend/react';
import { Button } from 'platform-bible-react';
import { type ReactElement, useState } from 'react';
import { LOCALIZED_STRING_KEYS } from '../types/localized-string-keys';
import type { AuthServerStatus } from '../utils/fw-lite-api';

/** Props for the AuthStatus component */
interface AuthStatusProps {
  servers?: AuthServerStatus[];
  login: (authority: string) => Promise<void>;
  logout: (authority: string) => Promise<void>;
}

/** Shows each configured Lexbox server's sign-in status with a Login/Logout button. */
export default function AuthStatus({
  servers,
  login,
  logout,
}: AuthStatusProps): ReactElement | undefined {
  const [localizedStrings] = useLocalizedStrings(LOCALIZED_STRING_KEYS);

  // Tracks which server's login/logout call is in flight so only that row's button disables.
  const [pendingAuthority, setPendingAuthority] = useState<string | undefined>();

  function handleLogin(authority: string): void {
    setPendingAuthority(authority);
    // eslint-disable-next-line promise/catch-or-return
    login(authority)
      .catch((e) => logger.error(localizedStrings['%lexicon_auth_loginError%'], JSON.stringify(e)))
      .finally(() => setPendingAuthority(undefined));
  }

  function handleLogout(authority: string): void {
    setPendingAuthority(authority);
    // eslint-disable-next-line promise/catch-or-return
    logout(authority)
      .catch((e) => logger.error(localizedStrings['%lexicon_auth_logoutError%'], JSON.stringify(e)))
      .finally(() => setPendingAuthority(undefined));
  }

  if (!servers?.length) return undefined;

  return (
    <div className="tw:flex tw:flex-col tw:gap-2 tw:p-4 tw:border-b">
      <h3 className="tw:font-semibold">{localizedStrings['%lexicon_auth_sectionTitle%']}</h3>
      {servers.map((status) => {
        const isPending = pendingAuthority === status.server.id;
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
