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

/** Matches messages/stack traces of the format:
System.InvalidOperationException: Everything is broken. Here's some ice cream.
   at FwLiteShared.Services.ProjectServicesProvider.OpenCrdtProject(String projectName)
 */
const dotnetErrorRegex = /^([\s\S]+?) {3}at /m;

function processErrorIntoDetails(message: string): {message: string, detail?: string} {
  const match = dotnetErrorRegex.exec(message);
  if (!match) return {message};
  return {message: match[1].trim(), detail: message.substring(match[1].length).trim()};
}

let setup = false;
export function setupGlobalErrorHandlers() {
  if (setup) return;
  setup = true;
  window.addEventListener('error', (event: ErrorEvent) => {
    console.error('Global error', event);

    if (suppressErrorNotification(event.message)) return;
    const {message: simpleMessage, detail} = processErrorIntoDetails(event.message);
    AppNotification.display(simpleMessage, {type: 'error', description: detail});
  });

  window.addEventListener('unhandledrejection', (event: PromiseRejectionEvent) => {
    const message = getPromiseRejectionMessage(event);
    //no need to log these because they already get logged by blazor.web.js

    if (suppressErrorNotification(message)) return;
    const {message: simpleMessage, detail} = processErrorIntoDetails(message);
    AppNotification.display(simpleMessage, {type: 'error', description: detail});
  });
}
