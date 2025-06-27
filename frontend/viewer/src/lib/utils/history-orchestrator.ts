import {delay} from './time';
import {useDebounce} from 'runed';

type HistoryChanger = () => { triggeredPopstate: true } | void;

type HistoryChange = {
  callback: HistoryChanger;
  key: string;
};

const historyQueue: HistoryChange[] = [];

const processHistory = useDebounce(async () => {
  while (historyQueue.length > 0) {
    console.debug(`Processing history change "${historyQueue[0].key}"`);
    await doHistoryChange(historyQueue.shift()!);
  }
}, 0);

let proccessingPromise: Promise<Promise<void>> | null = null;
export async function makeHistoryChange(callback: HistoryChanger, key: string): Promise<void> {
  const change: HistoryChange = { callback, key };
  historyQueue.push(change);
  if (proccessingPromise) {
    // If we're already processing, we just wait for it to finish
    return proccessingPromise;
  }

  proccessingPromise = processHistory();
  try {
    await proccessingPromise;
  } finally {
    proccessingPromise = null;
  }
}

async function doHistoryChange(change: HistoryChange): Promise<void> {
  const triggeredPopstate = change.callback();
  if (triggeredPopstate && triggeredPopstate.triggeredPopstate) {
    if (!await awaitPopstate()) {
      const message = `Timeout waiting for popstate event after history change "${change.key}".`;
      if (import.meta.env.DEV) {
        throw new Error(message);
      } else {
        console.warn(message);
      }
    }
  }
}

async function awaitPopstate(timeout = 1000): Promise<boolean> {
  const controller = new AbortController();
  const result = await Promise.any([
    new Promise<'popstate'>(resolve => {
      window.addEventListener('popstate', () => resolve('popstate'), {signal: controller.signal});
    }),
    delay(timeout),
  ]);
  controller.abort();
  return result === 'popstate';
}
