<script lang="ts">
  import {cn} from '$lib/utils';
  import type {HTMLAttributes} from 'svelte/elements';

  let {
    abbreviation,
    color,
    id,
    for: forId,
    class: className,
    ...restProps
  }: HTMLAttributes<HTMLElement> & {
    abbreviation: string;
    color?: string;
    id?: string;
    for?: string;
  } = $props();
</script>

<svelte:element
  this={forId ? 'label' : 'span'}
  for={forId}
  {id}
  class={cn('font-mono text-xs', color ?? 'text-muted-foreground', className)}
  {...restProps}>{abbreviation}</svelte:element
>
