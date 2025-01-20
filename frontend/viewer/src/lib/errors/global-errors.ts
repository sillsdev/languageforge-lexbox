import {AppNotification} from '$lib/notifications/notifications';

function getPromiseRejectionMessage(event: PromiseRejectionEvent): string {
  if (event.reason instanceof Error) {
    return event.reason.message;
  } else if (typeof event.reason === 'string') {
    return event.reason;
  } else {
    return 'Unknown error';
  }
}

function suppressPromiseRejectionNotification(message: string): boolean {
  if (message.includes('Perhaps the DotNetObjectReference instance was already disposed')) return true;
  return false;
}

export function setupGlobalErrorHandlers() {
  window.addEventListener('error', (event: ErrorEvent) => {
    console.error('Global error', event);
    AppNotification.display(event.message, 'error', undefined);
  });

  window.addEventListener('unhandledrejection', (event: PromiseRejectionEvent) => {
    const message = getPromiseRejectionMessage(event);
    console.error('Global unhandled rejection', message, event);

    if (suppressPromiseRejectionNotification(message)) return;
    AppNotification.display(message, 'error', undefined);
  });
}
