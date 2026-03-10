import {describe, expect, it, vi} from 'vitest';
import {mount, unmount} from 'svelte';
import type {ResourceReturn} from 'runed';
import LazyApiResourceHarness from './LazyApiResourceHarness.svelte';

describe('ProjectContext.lazyApiResource', () => {
  it('does not call API factory when lazy resource is only initialized', async () => {
    const fetchData = vi.fn(async () => ['loaded']);
    let resource: ResourceReturn<string[], unknown, true> | undefined;

    const app = mount(LazyApiResourceHarness, {
      target: document.body,
      props: {
        fetchData,
        onReady: (readyResource: ResourceReturn<string[], unknown, true>) => {
          resource = readyResource;
        },
      },
    });

    expect(resource).toBeDefined();
    expect(fetchData).not.toHaveBeenCalled();

    await unmount(app);
  });

  it('loads and propagates data after first current read', async () => {
    const fetchData = vi.fn(async () => ['loaded']);
    let resource: ResourceReturn<string[], unknown, true> | undefined;

    const app = mount(LazyApiResourceHarness, {
      target: document.body,
      props: {
        fetchData,
        onReady: (readyResource: ResourceReturn<string[], unknown, true>) => {
          resource = readyResource;
        },
      },
    });

    expect(resource).toBeDefined();

    // First read activates lazy resource but returns initial value until async load resolves.
    expect(resource!.current).toEqual([]);

    await vi.waitFor(() => {
      expect(fetchData).toHaveBeenCalledTimes(1);
      expect(resource!.current).toEqual(['loaded']);
    });

    await unmount(app);
  });
});
