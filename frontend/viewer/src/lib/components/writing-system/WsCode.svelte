<script lang="ts">
  import {cn} from '$lib/utils';
  import type {HTMLAttributes} from 'svelte/elements';

  let {
    abbreviation,
    color,
    id,
    for: forId,
    plain = false,
    class: className,
    ...restProps
  }: HTMLAttributes<HTMLElement> & {
    abbreviation: string;
    color?: string;
    id?: string;
    for?: string;
    /** Drop the chip chrome (border/background) for print-like contexts, e.g. the dictionary preview. */
    plain?: boolean;
  } = $props();
</script>

<svelte:element
  this={forId ? 'label' : 'span'}
  for={forId}
  {id}
  class={cn(
    'font-mono text-xs',
    !plain && 'rounded border border-border bg-muted/50 px-1',
    color ?? 'text-muted-foreground',
    className,
  )}
  {...restProps}>{abbreviation}</svelte:element
>
