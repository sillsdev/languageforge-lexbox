import {describe, expect, it, vi} from 'vitest';

import {delay} from './time';
import {queueHistoryChange, awaitPopstate, traverseHistory} from './history';
import {randomId} from '$lib/utils';

describe('queueHistoryChange', () => {
  it('processes changes that do not trigger popstate immediately', async () => {
    const processed: string[] = [];
    const change1 = queueTestHistoryChange(() => {
      processed.push('no-popstate');
      // No return value means no popstate expected
    });
    const change2 = queueTestHistoryChange(() => {
      processed.push('also-no-popstate');
    });

    await Promise.all([change1, change2]);
    expect(processed).toEqual(['no-popstate', 'also-no-popstate']);
  });

  it('processes mixed popstate and non-popstate changes in order', async () => {
    const processed: string[] = [];

    const change1 = queueTestHistoryChange(() => {
      processed.push('first-no-popstate');
    });

    const change2 = queueTestHistoryChange(async () => {
      processed.push('second-with-popstate');
      await awaitPopstate(1000);
    });

    const change3 = queueTestHistoryChange(() => {
      processed.push('third-no-popstate');
    });

    await delay(0);
    expect(processed).toEqual(['first-no-popstate', 'second-with-popstate']);

    // Dispatch popstate to unblock the queue
    window.dispatchEvent(new PopStateEvent('popstate'));

    await Promise.all([change1, change2, change3]);
    expect(processed).toEqual(['first-no-popstate', 'second-with-popstate', 'third-no-popstate']);
  });

  it('waits for popstate if requested', async () => {
    const processed: string[] = [];
    let teardownBResolved = false;
    const teardownB = queueTestHistoryChange(async () => {
      processed.push('teardown');
      await awaitPopstate(1000);
    }).then(() => {
      teardownBResolved = true;
    })
    void queueTestHistoryChange(() => { processed.push('setup'); });
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
    const firstChange = queueTestHistoryChange(async () => {
      processed.push('first');
      await awaitPopstate(1000);
    }).then(() => {
      firstResolved = true;
    });
    await delay(0);
    // The queue has started
    expect(processed).toEqual(['first']);
    // and is waiting for the popstate event
    expect(firstResolved).toBe(false);

    // Now we add a second change
    void queueTestHistoryChange(() => { processed.push('second'); });
    await delay(50);
    // the second change doesn't jump the queue of the current processing
    expect(processed).toEqual(['first']);

    window.dispatchEvent(new PopStateEvent('popstate'));
    await firstChange;
    // they finish together
    expect(processed).toEqual(['first', 'second']);
  });

  it('keeps the queue blocked on a real traverseHistory until popstate', async () => {
    const processed: string[] = [];
    const first = queueTestHistoryChange(() => traverseHistory(-1));
    const second = queueTestHistoryChange(() => { processed.push('after'); });

    // well past the old 100ms race window, with no popstate yet
    await delay(150);
    expect(processed).toEqual([]);

    window.dispatchEvent(new PopStateEvent('popstate'));
    await Promise.all([first, second]);
    expect(processed).toEqual(['after']);
  });

  it('handles empty queue gracefully', async () => {
    // This test ensures that the queue processing doesn't break when there are no items
    const processed: string[] = [];

    // Add and immediately process a single change
    const change = queueTestHistoryChange(() => {
      processed.push('single-change');
    });

    await change;
    expect(processed).toEqual(['single-change']);

    // Add a small delay to ensure the queue processing is completely finished
    await delay(10);

    // Queue should be empty now, adding another change should work fine
    const change2 = queueTestHistoryChange(() => {
      processed.push('second-change');
    });

    await change2;
    expect(processed).toEqual(['single-change', 'second-change']);
  });
});

describe('awaitPopstate', () => {

  it('resolves true when popstate event is dispatched before timeout', async () => {
    const popstatePromise = awaitPopstate(200);

    // Dispatch popstate event after a short delay
    setTimeout(() => {
      window.dispatchEvent(new PopStateEvent('popstate'));
    }, 50);

    const result = await popstatePromise;
    expect(result).toBe(true);
  });

  it('resolves false when timeout is reached before popstate event', async () => {
    const result = await awaitPopstate(50);
    expect(result).toBe(false);
  });

  it('handles multiple simultaneous awaitPopstate calls', async () => {
    const promise1 = awaitPopstate(200);
    const promise2 = awaitPopstate(200);
    const promise3 = awaitPopstate(200);

    // Dispatch popstate after a short delay
    setTimeout(() => {
      window.dispatchEvent(new PopStateEvent('popstate'));
    }, 50);

    const results = await Promise.all([promise1, promise2, promise3]);
    expect(results).toEqual([true, true, true]);
  });

  it('handles very short timeout values', async () => {
    const result = await awaitPopstate(1);
    expect(result).toBe(false);
  });

  it('handles zero timeout', async () => {
    const result = await awaitPopstate(0);
    expect(result).toBe(false);
  });

  it('handles rapid successive popstate events correctly', async () => {
    const promise1 = awaitPopstate(200);

    // Dispatch multiple popstate events rapidly
    window.dispatchEvent(new PopStateEvent('popstate'));
    window.dispatchEvent(new PopStateEvent('popstate'));
    window.dispatchEvent(new PopStateEvent('popstate'));

    const result = await promise1;
    expect(result).toBe(true);
  });
});

describe('traverseHistory', () => {
  it('calls history.go with the delta and resolves once popstate fires', async () => {
    const go = vi.spyOn(history, 'go');
    try {
      const traversal = traverseHistory(-1, 1000);
      expect(go).toHaveBeenCalledWith(-1);
      window.dispatchEvent(new PopStateEvent('popstate'));
      await expect(traversal).resolves.toBeUndefined();
    } finally {
      go.mockRestore();
    }
  });

  it('reports a lost traversal once the timeout elapses', async () => {
    // no popstate is dispatched, so the hang-breaker fires
    const traversal = traverseHistory(-1, 20);
    if (import.meta.env.DEV) {
      await expect(traversal).rejects.toThrow(/did not complete/);
    } else {
      await expect(traversal).resolves.toBeUndefined();
    }
  });
});

function queueTestHistoryChange(callback: () => void | Promise<void>): Promise<void> {
  return queueHistoryChange(callback, randomId());
}
