<script lang="ts">
  import { ActionBadge } from '../Badges';
  import type { Filter } from './FilterBar.svelte';

  interface Props {
    filter: Filter;
    children?: import('svelte').Snippet;
  }

  let { filter, children }: Props = $props();
</script>

<ActionBadge actionIcon="i-mdi-close" on:action={filter.clear}>
  {@render children?.()}
</ActionBadge>
