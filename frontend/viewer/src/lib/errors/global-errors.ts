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

function suppressErrorNotification(message: string): boolean {
  if (message.includes('Perhaps the DotNetObjectReference instance was already disposed')) return true;
  // Code (i.e. {expression}) inside a <MenuItem> slot, inside a portal causes this error if the portal is open while the screen is resized 🙃
  // It's worth noting that in Lexbox we've also seen browser extensions trigger this error
  if (message.includes('ResizeObserver loop completed with undelivered notifications')) return true;
  return false;
}

export function setupGlobalErrorHandlers() {
  window.addEventListener('error', (event: ErrorEvent) => {
    console.error('Global error', event);

    if (suppressErrorNotification(event.message)) return;
    AppNotification.display(event.message, 'error', undefined);
  });

  window.addEventListener('unhandledrejection', (event: PromiseRejectionEvent) => {
    const message = getPromiseRejectionMessage(event);
    console.error('Global unhandled rejection', message, event);

    if (suppressErrorNotification(message)) return;
    AppNotification.display(message, 'error', undefined);
  });
}
