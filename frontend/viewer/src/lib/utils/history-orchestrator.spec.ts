import {describe, expect, it} from 'vitest';

import {delay} from './time';
import {makeHistoryChange} from './history-orchestrator';
import {randomId} from '$lib/utils';

describe('HistoryOrchestrator scheduling', () => {
  it('processes history changes in batches', async () => {
    let processedCount = 0;
    const firstChange = queueHistoryChange(() => processedCount++);
    expect(processedCount).toBe(0);
    void queueHistoryChange(() => processedCount++);
    await firstChange;
    expect(processedCount).toBe(2);
  });

  it('waits for popstate if requested', async () => {
    const processed: string[] = [];
    let teardownBResolved = false;
    const teardownB = queueHistoryChange(() => {
      processed.push('teardown');
      return {triggeredPopstate: true};
    }).then(() => {
      teardownBResolved = true;
    })
    void queueHistoryChange(() => processed.push('setup'));
    await delay(0);
    expect(processed).toEqual(['teardown']);
    expect(teardownBResolved).toBe(false);
    window.dispatchEvent(new PopStateEvent('popstate'));
    await teardownB;
    expect(processed).toEqual(['teardown', 'setup']);
  });

  it('processes late changes after the current queue is complete', async () => {
    const processed: string[] = [];
    let firstResolved = false;
    const firstChange = queueHistoryChange(() => {
      processed.push('first');
      return {triggeredPopstate: true};
    }).then(() => {
      firstResolved = true;
    });
    await delay(0);
    // The queue has started
    expect(processed).toEqual(['first']);
    // and is waiting for the popstate event
    expect(firstResolved).toBe(false);

    // Now we add a second change
    void queueHistoryChange(() => processed.push('second'));
    await delay(100);
    // the second change doesn't jump the queue of the current processing
    expect(firstResolved).toBe(false);
    expect(processed).toEqual(['first']);

    window.dispatchEvent(new PopStateEvent('popstate'));
    await firstChange;
    // they finish together
    expect(processed).toEqual(['first', 'second']);
  });
});

function queueHistoryChange(callback: () => void): Promise<void> {
  return makeHistoryChange(callback, randomId());
}
