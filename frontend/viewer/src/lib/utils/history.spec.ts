import {describe, expect, it} from 'vitest';

import {delay} from './time';
import {queueHistoryChange} from './history';
import {randomId} from '$lib/utils';

describe('queueHistoryChange', () => {

  it('waits for popstate if requested', async () => {
    const processed: string[] = [];
    let teardownBResolved = false;
    const teardownB = queueTestHistoryChange(() => {
      processed.push('teardown');
      return {triggeredPopstate: true};
    }).then(() => {
      teardownBResolved = true;
    })
    void queueTestHistoryChange(() => processed.push('setup'));
    await delay(0);
    expect(processed).toEqual(['teardown']);
    expect(teardownBResolved).toBe(false);
    window.dispatchEvent(new PopStateEvent('popstate'));
    await teardownB;
    expect(processed).toEqual(['teardown', 'setup']);
  });

  it('processes late changes after the current change is complete', async () => {
    const processed: string[] = [];
    let firstResolved = false;
    const firstChange = queueTestHistoryChange(() => {
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
    void queueTestHistoryChange(() => processed.push('second'));
    await delay(50);
    // the second change doesn't jump the queue of the current processing
    expect(processed).toEqual(['first']);

    window.dispatchEvent(new PopStateEvent('popstate'));
    await firstChange;
    // they finish together
    expect(processed).toEqual(['first', 'second']);
  });
});

function queueTestHistoryChange(callback: () => void): Promise<void> {
  return queueHistoryChange(callback, randomId());
}
