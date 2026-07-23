<script lang="ts">
  import {cn} from '$lib/utils';
  import type {HTMLAttributes} from 'svelte/elements';

  let {
    abbreviation,
    id,
    for: forId,
    plain = false,
    class: className,
    ...restProps
  }: HTMLAttributes<HTMLElement> & {
    abbreviation: string;
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
    // The plain form is a passive annotation in the read-only dictionary/activity views, so it recedes at
    // foreground/60 — chosen over text-muted-foreground, which lands ~4.4:1 on the muted surface, under the
    // 4.5:1 AA floor for this 12px text. The chip is a field's writing-system label in the editor, where a
    // misread means data entered under the wrong writing system, so it runs full contrast (both themes).
    plain ? 'text-foreground/60' : 'text-foreground',
    className,
  )}
  {...restProps}>{abbreviation}</svelte:element
>
