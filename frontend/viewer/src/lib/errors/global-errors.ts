import {AppNotification} from '$lib/notifications/notifications';
import type {IJsInvokableLogger} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IJsInvokableLogger';
import {LogLevel} from '$lib/dotnet-types/generated-types/Microsoft/Extensions/Logging/LogLevel';
import {delay} from '$lib/utils/time';
import {useJsInvokableLogger} from '$lib/services/js-invokable-logger';

type UnifiedErrorEvent = {
  message: string;
  error: unknown;
  at?: string;
}

function unifyErrorEvent(event: ErrorEvent | PromiseRejectionEvent | Error): UnifiedErrorEvent {
  if (event instanceof Error) {
    return { message: event.message, error: event };
  } else if ('message' in event) {
    return { message: event.message, error: event.error, at: `${event.filename}:${event.lineno}:${event.colno}` };
  } else if (typeof event.reason === 'string') {
    return { message: event.reason, error: null };
  } else if (event.reason instanceof Error) {
    return { message: event.reason.message, error: event.reason };
  } else {
    return { message: 'Unknown error', error: event.reason };
  }
}

function shouldIgnoreError(message: string): boolean {
  if (message.includes('Perhaps the DotNetObjectReference instance was already disposed')) return true;
  // Blazor WebView completing a JS->.NET call whose JS-side registry was already torn down (e.g. page refresh).
  if (message.includes('There is no pending async call with ID')) return true;
  // Code (i.e. {expression}) inside a <MenuItem> slot, inside a portal causes this error if the portal is open while the screen is resized 🙃
  // It's worth noting that in Lexbox we've also seen browser extensions trigger this error
  if (message.includes('ResizeObserver loop completed with undelivered notifications')) return true;
  // Harmless error that seems to be caused by animate-out css inside a portal that is removed from the DOM.
  // Tried hard to make a repro for bits-ui, but failed. Occurs whenever a dropdown (e.g.) is open during navigation or similar.
  if (message.includes('this.opts.onOpenChangeComplete.current')) return true;
  return false;
}

/** Splits a .NET error string into its leading message and the rest (stack/inner exceptions), at whichever
comes first: the first stack frame ("   at ") or the first inner-exception marker (" ---> "). The latter matters
because .NET prints the whole inner-exception chain before the outer frames, so without it a deeply-wrapped
error (e.g. MSAL wrapping an Android network failure) dumps the entire cascade into the title.
System.InvalidOperationException: Everything is broken. Here's some ice cream.
   at FwLiteShared.Services.ProjectServicesProvider.OpenCrdtProject(String projectName)
 */
const dotnetErrorRegex = /^([\s\S]+?)(?: {3}at | ---> )/m;

export function processErrorIntoDetails(event: UnifiedErrorEvent): {message: string, detail?: string} {
  const message = event.message;
  const match = dotnetErrorRegex.exec(message);
  if (match) return {message: match[1].trim(), detail: message.substring(match[1].length).trim()};
  // A JS Error's stack repeats the message on its first line(s); use the prefix-free error message as the
  // title and just the frames as the detail, so the message isn't shown in both slots.
  else if (event.error instanceof Error) return {message: event.error.message || message, detail: stackFrames(event.error)};
  else return {message};
}

function stackFrames(error: Error): string | undefined {
  const stack = error.stack;
  if (!stack) return undefined;
  // V8 stacks begin with error.toString() ("Error: <message>"); strip it so the detail is frames only.
  // Other engines (Firefox/Safari) don't prepend it, so the stack is already frame-only — leave it as-is.
  const header = error.toString();
  return stack.startsWith(header) ? stack.slice(header.length).trim() : stack;
}

let setup = false;
export function setupGlobalErrorHandlers() {
  if (setup) return;
  setup = true;
  window.addEventListener('error', onErrorEvent);
  window.addEventListener('unhandledrejection', onErrorEvent);
}

export function onErrorEvent(event: ErrorEvent | PromiseRejectionEvent | Error) {
  const errorEvent = unifyErrorEvent(event);
  if (shouldIgnoreError(errorEvent.message)) return;
  void tryLogErrorToDotNet(errorEvent);
  const {message: simpleMessage, detail} = processErrorIntoDetails(errorEvent);
  AppNotification.error(simpleMessage, detail);
}

async function tryLogErrorToDotNet(error: UnifiedErrorEvent) {
  try {
    const details = getErrorString(error);
    if (!safeToLogErrorToDotNet(details)) return;
    const logger = await tryGetLogger();
    if (logger) await logger.log(LogLevel.Error, details);
    else console.warn('No DotNet logger available to log error', error);
  } catch (err) {
    console.error('Failed to log error to DotNet', err);
  }
}

function safeToLogErrorToDotNet(details: string): boolean {
  // likely cyclical errors
  if (details.includes('JsInvokableLogger')) return false;
  if (details.includes('tryLogErrorToDotNet')) return false;
  // dotnet is not available (can also be cyclical)
  if (details.includes('Cannot send data if the connection is not in the \'Connected\' State')) return false;
  return true;
}

// some very cheap durability.
// As it is today, the logger service is available before our error handlers are registered
async function tryGetLogger(): Promise<IJsInvokableLogger | undefined> {
  let logger = useJsInvokableLogger();
  if (logger) return logger;
  await delay(1);
  logger = useJsInvokableLogger();
  if (logger) return logger;
  await delay(1000);
  logger = useJsInvokableLogger();
  return logger;
}

function getErrorString(event: UnifiedErrorEvent) {
  const details = [`Message: ${event.message}`];
  if (event.at) details.push(`at ${event.at}`);
  if (event.error instanceof Error) {
    const error: Error = event.error;
    if (error.stack) details.push(`Stack: ${error.stack}`);
    if (error.cause) details.push(`Cause: ${tryStringify(error.cause)}`);
  } else if (event.error) {
    details.push(`Error: ${tryStringify(event.error)}`);
  }

  return details.join('\n');
}

function tryStringify(value: unknown): string | undefined {
  try {
    return JSON.stringify(value);
  } catch {
    return '(failed-to-stringify)';
  }
}
