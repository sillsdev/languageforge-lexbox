import {describe, expect, it, vi} from 'vitest';

import {LazyResource} from './lazy-resource';
import type {ResourceReturn} from 'runed';

describe('LazyResource', () => {
  it('activates exactly once on first current read', () => {
    const onActivate = vi.fn();
    const wrapped: ResourceReturn<number, string, true> = {
      get current() {
        return 7;
      },
      get loading() {
        return false;
      },
      get error() {
        return undefined;
      },
      mutate: vi.fn(),
      refetch: vi.fn(() => Promise.resolve(7)),
    };

    const lazy = new LazyResource(wrapped, onActivate);

    expect(onActivate).not.toHaveBeenCalled();
    expect(lazy.current).toBe(7);
    expect(lazy.current).toBe(7);
    expect(onActivate).toHaveBeenCalledTimes(1);
  });

  it('activates on first refetch when current has not been read', async () => {
    const onActivate = vi.fn();
    const wrapped: ResourceReturn<number, string, true> = {
      current: 0,
      loading: false,
      error: undefined,
      mutate: vi.fn(),
      refetch: vi.fn(() => Promise.resolve(99)),
    };

    const lazy = new LazyResource(wrapped, onActivate);
    await lazy.refetch('manual');

    expect(onActivate).toHaveBeenCalledTimes(1);
    expect(wrapped.refetch).toHaveBeenCalledWith('manual');
  });
});
