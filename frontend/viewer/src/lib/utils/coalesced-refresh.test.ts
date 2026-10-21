import {describe, expect, it, vi} from 'vitest';
import {CoalescedRefresh} from './coalesced-refresh';

function deferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (reason: unknown) => void;
  const promise = new Promise<T>((res, rej) => {
    resolve = res;
    reject = rej;
  });
  return {promise, resolve, reject};
}

// A fake slow server: each call returns a promise the test resolves by hand.
function fakeFetcher() {
  const calls: ReturnType<typeof deferred<string>>[] = [];
  let latest: string | undefined;
  const refresh = new CoalescedRefresh(async () => {
    const call = deferred<string>();
    calls.push(call);
    latest = await call.promise;
  });
  return {refresh, calls, get latest() { return latest; }};
}

describe('CoalescedRefresh', () => {
  it('joins the in-flight call instead of starting another', async () => {
    const f = fakeFetcher();
    const first = f.refresh.run();
    const second = f.refresh.run();
    expect(f.calls).toHaveLength(1);
    f.calls[0].resolve('a');
    await Promise.all([first, second]);
    expect(f.latest).toBe('a');
  });

  it('starts a new call once the previous one is done', async () => {
    const f = fakeFetcher();
    const first = f.refresh.run();
    f.calls[0].resolve('a');
    await first;
    const second = f.refresh.run();
    expect(f.calls).toHaveLength(2);
    f.calls[1].resolve('b');
    await second;
    expect(f.latest).toBe('b');
  });

  it('forceFresh resolves only after a call that started after the in-flight one', async () => {
    const f = fakeFetcher();
    void f.refresh.run();
    let freshDone = false;
    const fresh = f.refresh.run({forceFresh: true}).then(() => freshDone = true);
    f.calls[0].resolve('stale');
    await vi.waitFor(() => expect(f.calls).toHaveLength(2));
    expect(f.latest).toBe('stale');
    expect(freshDone).toBe(false);
    f.calls[1].resolve('fresh');
    await fresh;
    expect(f.latest).toBe('fresh');
  });

  it('queues a single follow-up however many forceFresh calls arrive', async () => {
    const f = fakeFetcher();
    void f.refresh.run();
    const a = f.refresh.run({forceFresh: true});
    const b = f.refresh.run({forceFresh: true});
    const c = f.refresh.run();
    expect(a).toBe(b);
    expect(c).toBe(a);
    f.calls[0].resolve('stale');
    await vi.waitFor(() => expect(f.calls).toHaveLength(2));
    f.calls[1].resolve('fresh');
    await a;
    expect(f.calls).toHaveLength(2);
    expect(f.latest).toBe('fresh');
  });

  it('runs the forced follow-up even when the in-flight call fails', async () => {
    const f = fakeFetcher();
    const failing = f.refresh.run();
    const fresh = f.refresh.run({forceFresh: true});
    f.calls[0].reject(new Error('server down'));
    await expect(failing).rejects.toThrow('server down');
    await vi.waitFor(() => expect(f.calls).toHaveLength(2));
    f.calls[1].resolve('fresh');
    await fresh;
    expect(f.latest).toBe('fresh');
  });
});

