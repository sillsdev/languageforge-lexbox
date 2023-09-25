<script lang="ts">
  import { ActionBadge } from '../Badges';
  import type { Filter } from './FilterBar.svelte';

  export let filter: Filter;
</script>

<ActionBadge size="badge-md" actionIcon="i-mdi-close" on:action={filter.clear}>
  <slot />
</ActionBadge>
