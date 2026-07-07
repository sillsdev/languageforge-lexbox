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
    // Not text-muted-foreground: on the muted chip fill (and muted preview surfaces) that lands at ~4.4:1,
    // under the 4.5:1 WCAG AA floor for this 12px text. foreground/60 keeps the same light-grey look while
    // clearing AA on every surface in both themes (verified: worst case ~5.1:1 on the light muted fill).
    color ?? 'text-foreground/60',
    className,
  )}
  {...restProps}>{abbreviation}</svelte:element
>
