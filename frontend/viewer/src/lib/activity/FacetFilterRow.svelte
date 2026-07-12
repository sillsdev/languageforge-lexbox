<script lang="ts">
  import type {Snippet} from 'svelte';
  import {t} from 'svelte-i18n-lingui';
  import {Checkbox} from '$lib/components/ui/checkbox';

  // One selectable row of a facet filter: checkbox + label + the hover "Only" shortcut that replaces
  // the whole selection with just this row.
  let {checked, onToggle, onOnly, label, indent = false, children}: {
    checked: boolean;
    onToggle: () => void;
    onOnly: () => void;
    /** Plain-text label; used for the checkbox aria-label. Rich content can be passed via children instead. */
    label: string;
    indent?: boolean;
    children?: Snippet;
  } = $props();
</script>

<div class="group/row flex items-center gap-2 rounded-sm py-1.5 pe-2 hover:bg-accent {indent ? 'ps-7' : 'ps-2'}">
  <Checkbox {checked} onCheckedChange={onToggle} aria-label={label} />
  <button type="button" class="grow text-start text-sm" onclick={onToggle}>
    {#if children}{@render children()}{:else}{label}{/if}
  </button>
  <button type="button"
    class="invisible text-xs text-muted-foreground hover:text-foreground group-hover/row:visible"
    onclick={onOnly}>
    {$t`Only`}
  </button>
</div>
