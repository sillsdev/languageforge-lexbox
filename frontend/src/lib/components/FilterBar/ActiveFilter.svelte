<script lang="ts">
  import { ActionBadge } from '../Badges';
  import { getContext } from 'svelte';
  import { RESET_FILTER_KEY } from './FilterBar.svelte';

  type Filter = { value: unknown; key: string };
  export let filter: Filter;
  const resetFilter = getContext<(f: Filter) => void>(RESET_FILTER_KEY);
</script>

<ActionBadge size="badge-md" actionIcon="i-mdi-close" on:action={() => resetFilter(filter)}>
  <slot />
</ActionBadge>
