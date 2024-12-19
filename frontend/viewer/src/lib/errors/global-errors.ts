import {AppNotification} from '$lib/notifications/notifications';

export function setupGlobalErrorHandlers() {
  window.addEventListener('error', (event: ErrorEvent) => {
    AppNotification.display(event.message, 'error', undefined);
  });

  window.addEventListener('unhandledrejection', (event: PromiseRejectionEvent) => {
    AppNotification.display(event.reason as string, 'error', undefined);
  });
}
