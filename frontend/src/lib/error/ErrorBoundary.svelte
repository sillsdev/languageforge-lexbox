<script lang="ts">
  import type {Snippet} from 'svelte';
  import {ensureErrorIsTraced} from '$lib/otel';
  import ErrorRecovery from './ErrorRecovery.svelte';

  interface Props {
    children?: Snippet;
  }
  const {children}: Props = $props();

  function onerror(error: unknown, _reset: () => void): void {
    ensureErrorIsTraced(error, undefined, {['app.error.source']: 'svelte-boundary'});
    // never call _reset synchronously (Svelte svelte_boundary_reset_onerror)
  }
</script>

<svelte:boundary {onerror}>
  {@render children?.()}
  {#snippet failed(error, reset)}
    <ErrorRecovery {error} onRetry={reset} />
  {/snippet}
</svelte:boundary>
