<script lang="ts">
  import type { Snippet } from 'svelte';
  import { ActionBadge } from '../Badges';
  import type { Filter } from './FilterBar.svelte';

  interface Props {
    filter: Filter;
    children?: Snippet;
  }

  const { filter, children }: Props = $props();
</script>

<ActionBadge actionIcon="i-mdi-close" onAction={filter.clear}>
  {@render children?.()}
</ActionBadge>
