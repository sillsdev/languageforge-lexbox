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
    'font-mono',
    // Monospace renders ~one step smaller than the app's sans, so the chip uses text-sm to match the
    // text-xs-sans label floor it replaced; the plain form stays text-xs to recede inline beside the gloss.
    plain ? 'text-xs' : 'text-sm',
    !plain && 'rounded border border-border bg-muted/50 px-1',
    // The plain form is a subordinate annotation in the read-only dictionary/activity views, so it reads
    // lighter than the value text — but at foreground/75 (~9:1 on white and muted), not the AA-floor grey,
    // since the reader still parses it to tell which writing system each gloss is. The chip is a field's
    // writing-system label in the editor, where a misread writes data under the wrong writing system, so it
    // runs full contrast (both themes).
    plain ? 'text-foreground/75' : 'text-foreground',
    className,
  )}
  {...restProps}>{abbreviation}</svelte:element
>
