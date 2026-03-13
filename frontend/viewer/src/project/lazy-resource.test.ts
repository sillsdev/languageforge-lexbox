import {describe, expect, it, vi} from 'vitest';

import {LazyProjectResource} from './lazy-resource.svelte';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';

const fakeApi = {} as IMiniLcmJsInvokable;

describe('LazyProjectResource', () => {
  it('does not fetch until .current is read, then fetches exactly once', async () => {
    const factory = vi.fn(() => Promise.resolve(7));
    const res = new LazyProjectResource(0, factory, () => fakeApi);

    expect(factory).not.toHaveBeenCalled();

    expect(res.current).toBe(0);
    void res.current;
    void res.current;

    await vi.waitFor(() => {
      expect(factory).toHaveBeenCalledTimes(1);
      expect(res.current).toBe(7);
    });
  });

  it('does not fetch if api is not available', () => {
    const factory = vi.fn(() => Promise.resolve(7));
    const res = new LazyProjectResource(0, factory, () => undefined);

    void res.current;
    expect(factory).not.toHaveBeenCalled();
  });

  it('fetches on onApiChange only if already active', () => {
    const factory = vi.fn(() => Promise.resolve(7));
    const inactive = new LazyProjectResource(0, factory, () => undefined);
    const active = new LazyProjectResource(0, factory, () => undefined);

    void active.current; // activate

    inactive.onApiChange(fakeApi);
    expect(factory).not.toHaveBeenCalled();

    active.onApiChange(fakeApi);
    expect(factory).toHaveBeenCalledTimes(1);
  });

  it('activates on refetch even if current was never read', async () => {
    const factory = vi.fn(() => Promise.resolve(99));
    const res = new LazyProjectResource(0, factory, () => fakeApi);

    await res.refetch();
    expect(factory).toHaveBeenCalledTimes(1);
    expect(res.current).toBe(99);
  });

  it('tracks loading and error states', async () => {
    let resolve!: (v: number) => void;
    function factory() {
      return new Promise<number>(r => {
        resolve = r;
      });
    }
    const res = new LazyProjectResource(0, factory, () => fakeApi);

    void res.current;
    await vi.waitFor(() => expect(res.loading).toBe(true));

    resolve(42);
    await vi.waitFor(() => {
      expect(res.loading).toBe(false);
      expect(res.current).toBe(42);
      expect(res.error).toBeUndefined();
    });

    // error case
    const failing = new LazyProjectResource(0, () => Promise.reject(new Error('boom')), () => fakeApi);
    void failing.current;
    await vi.waitFor(() => {
      expect(failing.error?.message).toBe('boom');
    });
  });
});
