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
  showConsumer: () => void;
  destroyConsumer: () => void;
  swapApi: (api: IMiniLcmJsInvokable) => void;
};

describe('ProjectContext.lazyApiResource', () => {
  it('does not call API factory when lazy resource is only initialized', async () => {
    const fetchData = vi.fn(() => Promise.resolve(['loaded']));
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

    // Activate via consumer, then immediately destroy it.
    controls!.showConsumer();

    await vi.waitFor(() => {
      expect(fetchData).toHaveBeenCalledTimes(1);
    });

    controls!.destroyConsumer();

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

    // Activate consumer
    controls!.showConsumer();

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
