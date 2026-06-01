import {describe, expect, it, vi} from 'vitest';
import {mount, unmount} from 'svelte';

import type {WrappedHarnessControls} from './detached-api-resource-test-types';
import WrappedResourceHarness from './WrappedResourceHarness.svelte';

/**
 * Reproduces the "wrapper service" pattern used by PartOfSpeechService and
 * the other cached data services: a class instance owns a DetachedResource
 * and exposes a `$derived.by` view of it; UI consumers read that derived
 * view from inside their OWN `$derived` callback. The instance is cached
 * per-projectContext via `getOrAdd`, so multiple component lifecycles can
 * see the same instance.
 *
 * Live regression (commit a179ecfc3): PartOfSpeechService.current stayed
 * `[]` even after the resource fetch resolved, so DictionaryEntry's PoS
 * labels never rendered. Cause: Svelte 5.55.3 "freeze deriveds once their
 * containing effects are destroyed" — the class-field `$derived.by` was
 * owned by whichever effect root happened to be active when the service
 * was first constructed (typically a transient component like a
 * virtualized EntryRow). When that component unmounted, the derived
 * froze for every other reader of the cached service.
 *
 * Fix: `getOrAdd` runs the factory inside a context-owned `$effect.root`,
 * so the derived's owning root lives as long as the project context.
 */
function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>(r => {
    resolve = r;
  });
  return {promise, resolve};
}

function mountHarness(fetchData: () => Promise<string[]>, lookupKey = 'beta') {
  let controls: WrappedHarnessControls;
  const app = mount(WrappedResourceHarness, {
    target: document.body,
    props: {
      fetchData,
      lookupKey,
      onReady: (c: WrappedHarnessControls) => { controls = c; },
    },
  });
  return {app, get controls() { return controls; }};
}

function consumerText(slot: 'first-consumer' | 'second-consumer', field: 'consumer-count' | 'consumer-found') {
  return document.querySelector(`[data-testid="${slot}"] [data-testid="${field}"]`)?.textContent;
}

describe('Wrapper service over apiResource', () => {
  it('consumer renders the wrapper-transformed value once the resource resolves', async () => {
    const fetchData = vi.fn(() => Promise.resolve(['alpha', 'beta', 'gamma']));
    const {app, controls} = mountHarness(fetchData, 'beta');

    controls.showConsumer();
    await vi.waitFor(() => {
      expect(consumerText('first-consumer', 'consumer-count')).toBe('3');
      expect(consumerText('first-consumer', 'consumer-found')).toBe('BETA');
    });
    expect(fetchData).toHaveBeenCalledTimes(1);

    await unmount(app);
  });

  it('REGRESSION: cached wrapper stays reactive after its creator unmounts', async () => {
    // The canonical bug: a transient component creates the cached service,
    // the service's $derived.by gets owned by that component's effect root,
    // then the component unmounts before the fetch resolves. With the bug,
    // the derived freezes at [] for everyone else.
    const pending = deferred<string[]>();
    const fetchData = vi.fn(() => pending.promise);
    const {app, controls} = mountHarness(fetchData, 'beta');

    controls.showConsumer();
    await vi.waitFor(() => {
      expect(fetchData).toHaveBeenCalledTimes(1);
    });
    controls.destroyConsumer();

    pending.resolve(['alpha', 'beta', 'gamma']);

    controls.showSecondConsumer();
    await vi.waitFor(() => {
      expect(consumerText('second-consumer', 'consumer-count')).toBe('3');
      expect(consumerText('second-consumer', 'consumer-found')).toBe('BETA');
    });

    await unmount(app);
  });

  it('REGRESSION: cached wrapper reacts to api swaps after its creator unmounts', async () => {
    // Real-world trigger for the same freeze: project switch re-runs the
    // resource fetch on a long-lived cached service whose creator (e.g. a
    // virtualized row) is no longer mounted.
    const fetchData = vi.fn()
      .mockResolvedValueOnce(['first'])
      .mockResolvedValueOnce(['second']);
    const {app, controls} = mountHarness(fetchData, 'second');

    controls.showConsumer();
    await vi.waitFor(() => {
      expect(consumerText('first-consumer', 'consumer-count')).toBe('1');
    });
    controls.destroyConsumer();

    controls.swapApi({} as never);

    controls.showSecondConsumer();
    await vi.waitFor(() => {
      expect(consumerText('second-consumer', 'consumer-count')).toBe('1');
      expect(consumerText('second-consumer', 'consumer-found')).toBe('SECOND');
    });

    await unmount(app);
  });
});
