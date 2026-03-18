import {describe, expect, it, vi} from 'vitest';
import {mount, unmount} from 'svelte';

import type {HarnessControls} from './detached-api-resource-test-types';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';
import DetachedApiResourceHarness from './DetachedApiResourceHarness.svelte';

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>(r => {
    resolve = r;
  });
  return {promise, resolve};
}

function mountHarness(fetchData: () => Promise<string[]>) {
  let controls: HarnessControls;
  const app = mount(DetachedApiResourceHarness, {
    target: document.body,
    props: {
      fetchData,
      onReady: (c: HarnessControls) => { controls = c; },
    },
  });
  return { app, get controls() { return controls; } };
}

describe('ProjectContext.detachedApiResource', () => {
  it('continues loading if first consumer is destroyed before load completes', async () => {
    const pending = deferred<string[]>();
    const fetchData = vi.fn(() => pending.promise);
    const {app, controls} = mountHarness(fetchData);

    controls.showConsumer();

    await vi.waitFor(() => {
      expect(fetchData).toHaveBeenCalledTimes(1);
    });

    controls.destroyConsumer();

    pending.resolve(['after-unmount']);

    await vi.waitFor(() => {
      expect(controls.resource.current).toEqual(['after-unmount']);
    });

    await unmount(app);
  });

  it('updates when api changes after first consumer is destroyed', async () => {
    const fetchData = vi.fn()
      .mockResolvedValueOnce(['first'])
      .mockResolvedValueOnce(['second']);

    const {app, controls} = mountHarness(fetchData);

    controls.showConsumer();

    await vi.waitFor(() => {
      expect(controls.resource.current).toEqual(['first']);
    });

    controls.destroyConsumer();
    controls.swapApi({} as IMiniLcmJsInvokable);

    await vi.waitFor(() => {
      expect(fetchData).toHaveBeenCalledTimes(2);
      expect(controls.resource.current).toEqual(['second']);
    });

    await unmount(app);
  });
});
