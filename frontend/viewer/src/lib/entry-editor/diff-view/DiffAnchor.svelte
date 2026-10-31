<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import DiffText from './DiffText.svelte';
  import DiffMarker from './DiffMarker.svelte';
  import HomographNumber from '$lib/components/dictionary/HomographNumber.svelte';
  import {diffAdded, diffRemoved} from './diff-classes';

  // Always shown, changed or not, so a diff narrowed to its changes still says which entity it is.
  let {before, after, beforeHomograph = 0, afterHomograph = 0, noVisibleChanges = false}: {
    before?: string;
    after?: string;
    beforeHomograph?: number;
    afterHomograph?: number;
    /** The change only touched what a diff doesn't show, e.g. formatting */
    noVisibleChanges?: boolean;
  } = $props();
</script>

{#snippet homographNumber(n: number, state?: 'added' | 'removed')}
  {#if n > 0}<span class="rounded-sm {state === 'added' ? diffAdded : state === 'removed' ? diffRemoved : ''}">{#if state}<DiffMarker {state} />{/if}<HomographNumber homographNumber={n} /></span>{/if}
{/snippet}

<div class="col-span-full text-lg font-semibold wrap-break-word" data-testid="diff-anchor">
  <!-- One line: whitespace would put a gap between the headword and its homograph number -->
  {#if !before && !after}<span class="text-muted-foreground">{$t`Untitled`}</span>{:else}<DiffText {before} {after} />{/if}{#if beforeHomograph === afterHomograph}{@render homographNumber(afterHomograph)}{:else}{@render homographNumber(beforeHomograph, 'removed')}{@render homographNumber(afterHomograph, 'added')}{/if}
</div>
{#if noVisibleChanges}
  <!-- Styled like the preview's "Preview not available" -->
  <div class="col-span-full text-muted-foreground text-center">{$t`No visible changes`}</div>
{/if}
