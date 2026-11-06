import {delay} from './time';

type HistoryChanger = () => { triggeredPopstate: true } | void;

type HistoryChange = {
  callback: HistoryChanger;
  key: string;
  resolve: () => void;
};

const historyQueue: HistoryChange[] = [];

let processingPromise: Promise<void> | null = null;
export function queueHistoryChange(callback: HistoryChanger, key: string): Promise<void> {
  const historyPromise = new Promise<void>(resolve => {
    const change = {callback, key, resolve};
    historyQueue.push(change);
  });
  // ensure the queue is being processed
  processingPromise ??= processHistory().finally(() => {
    processingPromise = null;
  });
  // we don't wait for the whole queue, just for our change
  return historyPromise;
}

async function processHistory() {
  while (historyQueue.length > 0) {
    const historyChange = historyQueue.shift()!;
    console.debug(`Processing history change "${historyChange.key}"`);
    await doHistoryChange(historyChange);
    historyChange.resolve();
  }
}

async function doHistoryChange(change: HistoryChange): Promise<void> {
  const result = change.callback();
  if (result && result.triggeredPopstate) {
    if (!await awaitPopstate()) {
      const message = `Timed out waiting for popstate event after history change "${change.key}".`;
      if (import.meta.env.DEV) {
        throw new Error(message);
      } else {
        console.warn(message);
      }
    }
  }
}

// iOS WKWebView can take well over 100ms to deliver popstate after history.back(). Timing out
// early lets the next queued change (e.g. a dialog's pushState) run first, and the late popstate
// is then taken as a real back press, closing that dialog. Callers only wait after a change that
// does trigger popstate, so a generous timeout costs nothing in the normal case.
export async function awaitPopstate(timeout = 1000): Promise<boolean> {
  const controller = new AbortController();
  const result = await Promise.any([
    new Promise<'popstate'>(resolve => {
      window.addEventListener('popstate', () => resolve('popstate'), {signal: controller.signal, once: true});
    }),
    delay(timeout),
  ]);
  controller.abort();
  return result === 'popstate';
}
