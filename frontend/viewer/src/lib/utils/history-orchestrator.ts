import {navigate} from 'svelte-routing';
import {useDebounce} from 'runed';

type HistoryChanger = () => { triggersPopstate: true } | void;

type HistoryChange = {
  callback: HistoryChanger;
  key: string;
  isTeardown: boolean;
};

const historyQueue: HistoryChange[] = [];

const processHistory = useDebounce(async () => {
  [...new Set(historyQueue.map(change => change.key))].forEach((value, _, items) => {
    if (items.filter(item => item === value).length > 1) {
      throw new Error(`Multiple history changes for key "${value}" detected.`);
    }
  });

  if (historyQueue.length === 0) return;

  while (historyQueue.length > 0) {
    const nextTeardown = historyQueue.find(change => change.isTeardown);
    if (nextTeardown) { // prioritize teardown
      if (historyQueue[0] !== nextTeardown) {
        console.debug(`Teardown "${nextTeardown.key}" jumped the queue.`);
      }
      await doHistoryChange(nextTeardown.callback);
      historyQueue.splice(historyQueue.indexOf(nextTeardown), 1);

    } else {
      await doHistoryChange(historyQueue.shift()!.callback);
    }
  }
}, 1);

export async function makeHistoryChange(callback: HistoryChanger, options: Omit<HistoryChange, 'callback'>): Promise<void> {
  const change: HistoryChange = { callback, ...options };
  historyQueue.push(change);
  await processHistory();
}

async function doHistoryChange(callback: HistoryChanger): Promise<void> {
  const triggersPopstate = callback();
  if (triggersPopstate && triggersPopstate.triggersPopstate) {
    await new Promise<void>(resolve => {
      const abortController = new AbortController();
      window.addEventListener('popstate', () => {
        // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment
        navigate(document.location.href, { replace: true, state: history.state });
        console.debug('History orchestrator: popstate event triggered');
        resolve();
        abortController.abort();
      }, {
        signal: abortController.signal,
      });
    });
  }
}
