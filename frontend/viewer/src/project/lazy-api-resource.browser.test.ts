import {describe, expect, it, vi} from 'vitest';
import {mount, unmount} from 'svelte';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
import LazyApiResourceHarness from './LazyApiResourceHarness.svelte';

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>(r => {
    resolve = r;
  });
  return {promise, resolve};
}

type HarnessControls = {
  resource: {
    current: string[];
  };
  destroyConsumer: () => void;
  swapApi: (api: IMiniLcmJsInvokable) => void;
};

describe('ProjectContext.lazyApiResource', () => {
  it('does not call API factory when lazy resource is only initialized', async () => {
    const fetchData = vi.fn(async () => ['loaded']);
    let controls: HarnessControls | undefined;

    const app = mount(LazyApiResourceHarness, {
      target: document.body,
      props: {
        fetchData,
        onReady: (readyControls: HarnessControls) => {
          controls = readyControls;
        },
      },
    });

    expect(controls).toBeDefined();
    expect(fetchData).not.toHaveBeenCalled();

    await unmount(app);
  });

  it('loads and propagates data after first current read', async () => {
    const fetchData = vi.fn(async () => ['loaded']);
    let controls: HarnessControls | undefined;

    const app = mount(LazyApiResourceHarness, {
      target: document.body,
      props: {
        fetchData,
        onReady: (readyControls: HarnessControls) => {
          controls = readyControls;
        },
      },
    });

    expect(controls).toBeDefined();

    // First read activates lazy resource but returns initial value until async load resolves.
    expect(controls!.resource.current).toEqual([]);

    await vi.waitFor(() => {
      expect(fetchData).toHaveBeenCalledTimes(1);
      expect(controls!.resource.current).toEqual(['loaded']);
    });

    await unmount(app);
  });

  it('continues loading if first consumer is destroyed before load completes', async () => {
    const pending = deferred<string[]>();
    const fetchData = vi.fn(() => pending.promise);
    let controls: HarnessControls | undefined;

    const app = mount(LazyApiResourceHarness, {
      target: document.body,
      props: {
        fetchData,
        onReady: (readyControls: HarnessControls) => {
          controls = readyControls;
        },
      },
    });

    expect(controls).toBeDefined();

    // Activate via first consumer, then immediately destroy it.
    expect(controls!.resource.current).toEqual([]);
    controls!.destroyConsumer();

    await vi.waitFor(() => {
      expect(fetchData).toHaveBeenCalledTimes(1);
    });

    pending.resolve(['after-unmount']);

    await vi.waitFor(() => {
      expect(controls!.resource.current).toEqual(['after-unmount']);
    });

    await unmount(app);
  });

  it('updates when api changes after first consumer is destroyed', async () => {
    const fetchData = vi.fn()
      .mockResolvedValueOnce(['first'])
      .mockResolvedValueOnce(['second']);

    let controls: HarnessControls | undefined;

    const app = mount(LazyApiResourceHarness, {
      target: document.body,
      props: {
        fetchData,
        onReady: (readyControls: HarnessControls) => {
          controls = readyControls;
        },
      },
    });

    expect(controls).toBeDefined();
    expect(controls!.resource.current).toEqual([]);

    await vi.waitFor(() => {
      expect(controls!.resource.current).toEqual(['first']);
    });

    controls!.destroyConsumer();
    controls!.swapApi({} as IMiniLcmJsInvokable);

    await vi.waitFor(() => {
      expect(fetchData).toHaveBeenCalledTimes(2);
      expect(controls!.resource.current).toEqual(['second']);
    });

    await unmount(app);
  });
});
