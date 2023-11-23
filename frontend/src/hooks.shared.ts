import { goHome } from '$lib/user';
import { redirect } from '@sveltejs/kit';

const sayWuuuuuuut = 'We\'re not sure what happened.';

export function getErrorMessage(error: unknown): string {
  if (error === null || error === undefined) {
    return sayWuuuuuuut;
  } else if (typeof error === 'string') {
    return error;
  }

  const _error = (error ?? {}) as Record<string, string>;
  return (
      _error.message ??
      _error.reason ??
      _error.cause ??
      _error.error ??
      _error.code ??
      sayWuuuuuuut
  );
}

export async function validateFetchResponse(response: Response, isAtLogin: boolean, isHome: boolean): Promise<void> {
  if (response.status === 401 && !isAtLogin) {
    throw redirect(307, '/logout');
  }

  if (response.status === 403) {
    if (isHome) {
      // the user's JWT appears to be invalid
      throw redirect(307, '/logout');
    } else {
      // the user tried to access something they don't have permission for
      await goHome();
    }
  }

  if (response.status >= 500) {
    throw new Error(`Unexpected response: ${response.statusText} (${response.status}). URL: ${response.url}.`);
  }
}
