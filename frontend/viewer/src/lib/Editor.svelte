<script lang="ts">
  import type { IEntry } from './mini-lcm';
  import EntryEditor from './entry-editor/EntryEditor.svelte';
  import type { Readable } from 'svelte/store';
  import { getContext } from 'svelte';
  import type { ViewConfig } from './config-types';

  export let entry: IEntry;

  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');
</script>

<div class="editor grid" style="grid-template-columns: 170px 40px 1fr" class:hide-empty-fields={$viewConfig.hideEmptyFields}>
  <div id="entry"></div>
  <div class="contents">
    <EntryEditor on:change={() => {entry = entry; if (entry) entry.senses = entry.senses;}} entry={entry} />
  </div>
</div>

<style lang="postcss">
  :global(.hide-empty-fields .empty) {
    display: none !important;
  }

  #entry, :global(.editor :is(h2, h3)) {
    scroll-margin-top: 1rem;
  }
</style>
