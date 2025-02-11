import {AppNotification} from '$lib/notifications/notifications';

const sayWuuuuuuut = 'We\'re not sure what happened.';

function getErrorMessage(error: unknown): string {
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

function getPromiseRejectionMessage(event: PromiseRejectionEvent): string {
  if (event.reason instanceof Error) {
    return event.reason.message;
  } else if (typeof event.reason === 'string') {
    return event.reason;
  } else {
    return 'Unknown error';
  }
}

function suppressErrorNotification(message: string): boolean {
  if (message.includes('Perhaps the DotNetObjectReference instance was already disposed')) return true;
  // Code (i.e. {expression}) inside a <MenuItem> slot, inside a portal causes this error if the portal is open while the screen is resized 🙃
  // It's worth noting that in Lexbox we've also seen browser extensions trigger this error
  if (message.includes('ResizeObserver loop completed with undelivered notifications')) return true;
  return false;
}

const dotnetErrorRegex = /^((?:.|\s)+?) {3}at /m;
function processErrorIntoDetails(message: string): {message: string, detail?: string} {
  const match = dotnetErrorRegex.exec(message);
  if (!match) return {message};
  return {message: match[1].trim(), detail: message.substring(match[1].length).trim()};
}

export function setupGlobalErrorHandlers() {
  window.addEventListener('error', (event: ErrorEvent) => {
    console.error('Global error', event);

    handleErrorMessage(event.message);
  });

  window.addEventListener('unhandledrejection', (event: PromiseRejectionEvent) => {
    const message = getPromiseRejectionMessage(event);
    //no need to log these because they already get logged by blazor.web.js

    handleErrorMessage(message);
  });
}

function handleErrorMessage(message: string) {
  if (suppressErrorNotification(message)) return;
  const {message: simpleMessage, detail} = processErrorIntoDetails(message);
  AppNotification.display(simpleMessage, 'error', undefined, detail);
}

export function isNoSuchHostError(error: unknown): boolean {
  const message = getErrorMessage(error);
  return message.includes('SocketException') && message.includes('(11001)');
}
