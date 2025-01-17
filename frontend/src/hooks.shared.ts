import {redirect} from '@sveltejs/kit';
import {tryGetErrorMessage} from '$lib/error/utils';

const sayWuuuuuuut = 'We\'re not sure what happened.';

export function getErrorMessage(error: unknown): string {
  return tryGetErrorMessage(error) ?? sayWuuuuuuut;
}

export function validateFetchResponse(
  response: Response, isHome: boolean, config?: LexboxResponseHandlingConfig): void {
  if (config?.disableRedirectOnAuthError && [401, 403].includes(response.status)) {
    return;
  }

  if (response.status === 401) {
    redirect(307, '/logout');
  }

  if (response.status === 403) {
    if (isHome) {
      // the user's JWT appears to be invalid
      redirect(307, '/logout');
    } else {
      // the user tried to access something they don't have permission for
      redirect(307, '/home');
    }
  }

  if (response.status >= 500) {
    throw new Error(`Unexpected response: ${response.statusText} (${response.status}). URL: ${response.url}.`);
  }
}
