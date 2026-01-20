<!--
	Delayed loading component that checks cache first, then delays actual loading.
	Useful for virtual scrolling where items might be destroyed before loading completes.

	Usage:
	<Delayed
		load={() => fetchEntry(id)}
		getCached={() => cache.get(id)}
		delay={150}
	>
		{#snippet children(state)}
			{#if state.loading}
				<Skeleton />
			{:else if state.error}
				<ErrorDisplay error={state.error} />
			{:else}
				<EntryView entry={state.current} />
			{/if}
		{/snippet}
	</Delayed>
-->
<script lang="ts" generics="T">
  import type {Snippet} from 'svelte';
  import {watch} from 'runed';

  type DelayedState<T> = {
    loading: boolean;
    current: T | undefined;
    error: unknown | undefined;
  };

  interface Props<T> {
    load: () => Promise<T>;
    getCached?: () => T | undefined;
    delay?: number;
    children: Snippet<[state: DelayedState<T>]>;
  }

  let { load, getCached, delay = 150, children }: Props<T> = $props();

  let state = $state<DelayedState<T>>({
    loading: true,
    current: undefined,
    error: undefined,
  });

  watch(
    () => [load, getCached],
    () => {
      cancelLoading();
      reset();

      // Check cache first
      if (getCached) {
        const cached = getCached();
        if (cached !== undefined) {
          state = { loading: false, current: cached, error: undefined };
          return;
        }
      }

      // Start delayed loading
      startLoading();

      return () => cancelLoading();
    },
  );

  function reset() {
    state = { loading: true, current: undefined, error: undefined };
  }

  let timeoutId: ReturnType<typeof setTimeout> | undefined;
  let runId = 0;

  function startLoading() {
    const currentRun = ++runId;
    reset();
    timeoutId = setTimeout(() => void doLoad(currentRun), delay);
  }

  async function doLoad(currentRun: number) {
    try {
      const result = await load();
      if (currentRun !== runId) {
        return;
      }
      state = { loading: false, current: result, error: undefined };
    } catch (error) {
      if (currentRun !== runId) {
        return;
      }
      state = { loading: false, current: undefined, error };
    }
  }

  function cancelLoading() {
    clearTimeout(timeoutId);
    timeoutId = undefined;
    runId += 1;
  }
</script>

{@render children(state)}
