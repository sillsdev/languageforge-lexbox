import {delay} from './time';

type HistoryChanger = () => void | Promise<void>;

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
    await historyChange.callback();
    historyChange.resolve();
  }
}

// popstate is dispatched as a task, so it can lag behind heavy synchronous work.
// Callers only traverse to an entry they've verified exists, so the event is
// guaranteed to arrive; this only caps a genuinely lost traversal, it doesn't
// gate the success path, so the exact value isn't load-bearing.
const TRAVERSAL_TIMEOUT = 1000;

export async function traverseHistory(delta: number, timeout = TRAVERSAL_TIMEOUT): Promise<void> {
  // attach the listener before triggering the traversal so a fast popstate can't be missed
  const traversed = awaitPopstate(timeout);
  history.go(delta);
  if (!await traversed) {
    const message = `History traversal (${delta}) did not complete within ${timeout}ms; the popstate event was lost.`;
    if (import.meta.env.DEV) {
      throw new Error(message);
    } else {
      console.error(message);
    }
  }
}

export async function awaitPopstate(timeout: number): Promise<boolean> {
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
