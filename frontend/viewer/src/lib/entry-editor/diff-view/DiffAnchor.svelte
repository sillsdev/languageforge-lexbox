<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import DiffText from './DiffText.svelte';
  import DiffMarker from './DiffMarker.svelte';
  import HomographNumber from '$lib/components/dictionary/HomographNumber.svelte';
  import {diffAdded, diffRemoved} from './diff-classes';
  import {cn} from '$lib/utils';
  import type {AnchorText} from './anchor-text';

  // Names the entity, changed or not, so a diff narrowed to its changes still says which entity it is.
  // Inline and unstyled: the activity preview puts it in its change header, a primitive in a heading row.
  let {before, after, beforeHomograph = 0, afterHomograph = 0, detail, class: className}: AnchorText & {class?: string} = $props();
</script>

<!-- Diff classes on the <sub> itself: on a wrapper, the tint fills the line above the digit and the strike misses it -->
{#snippet homographNumber(n: number, state?: 'added' | 'removed')}
  {#if n > 0}{#if state}<DiffMarker {state} />{/if}<HomographNumber homographNumber={n} class={cn(state && 'rounded-sm', state === 'added' && diffAdded, state === 'removed' && diffRemoved,
    // Keeps ₁₂ from reading as 12; a margin, so no line break
    state === 'added' && beforeHomograph > 0 && 'ms-1')} />{/if}
{/snippet}

<!-- One line: whitespace would put a gap between the headword and its homograph number -->
<span class={cn('wrap-break-word', className)} data-testid="diff-anchor">{#if !before && !after}<span class="text-muted-foreground">{$t`Untitled`}</span>{:else}<DiffText {before} {after} />{/if}{#if beforeHomograph === afterHomograph}{@render homographNumber(afterHomograph)}{:else}{@render homographNumber(beforeHomograph, 'removed')}{@render homographNumber(afterHomograph, 'added')}{/if}{#if detail}<span class="text-muted-foreground font-normal text-sm ms-2">{detail}</span>{/if}</span>
