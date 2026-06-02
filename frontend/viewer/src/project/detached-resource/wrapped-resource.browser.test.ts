import {describe, expect, it, vi} from 'vitest';
import {mount, unmount} from 'svelte';

import type {WrappedHarnessControls} from './detached-api-resource-test-types';
import WrappedResourceHarness from './WrappedResourceHarness.svelte';

/**
 * Regression cover for the cached-service staleness fixed in ProjectContext#ownAndCache
 * (live symptom: PartOfSpeechService.current stuck at `[]`, so PoS labels never
 * rendered). See that method for why context-owned reactive state is needed; the
 * cases below exercise the happy path plus the two unmount orderings that triggered it.
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
    // Pins the ordering: the creator unmounts *before* the fetch resolves.
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
    // Real-world trigger: a project switch re-runs the fetch after the creating row is gone.
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
