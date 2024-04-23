<script lang="ts">
  import { Button, ListItem, cls } from 'svelte-ux';
  import type { IEntry } from './mini-lcm';
  import { firstDefOrGlossVal, firstVal } from './utils';
  import EntryEditor from './entry-editor/EntryEditor.svelte';
  import DictionaryEntry from './DictionaryEntry.svelte';
  import { mdiChevronDown, mdiChevronUp } from '@mdi/js';
  import type { Readable } from 'svelte/store';
  import { getContext } from 'svelte';

  export let entries: IEntry[];

  const demoValues = getContext<Readable<any>>('demoValues');

  let selectedEntry: IEntry | undefined = entries[0];
  let dictionaryEntryLines: number;
  let expandDictionaryEntry = false;
</script>

<div class="grid grid-cols-subgrid col-span-3 flex-grow gap-8">
  <div class="min-w-48 pr-8 border-r-2">
    <div class="entry-list grid border rounded-md">
      {#each entries as entry}
        <ListItem
          title={firstVal(entry.lexemeForm)}
          subheading={firstDefOrGlossVal(entry.senses[0])}
          on:click={() => (selectedEntry = entry)}
          class={cls(
            'cursor-pointer',
            'hover:bg-surface-300',
            selectedEntry == entry ? 'bg-surface-200' : ''
          )}
          noShadow
        />
      {/each}
    </div>
  </div>

  {#if selectedEntry}
    <div id="entry" class="grid self-start" style="grid-template-columns: 170px 40px 1fr" class:hide-empty-fields={$demoValues.hideEmptyFields}>
      <div class="col-span-full relative mb-6">
        <div class="bg-neutral text-surface-content overflow-auto rounded-sm shadow shadow-neutral" class:max-h-20={!expandDictionaryEntry}>
          <div class="px-3 py-2 text-sm">
            <DictionaryEntry entry={selectedEntry} bind:lines={dictionaryEntryLines} />
          </div>
        </div>
        {#if dictionaryEntryLines > 3}
            <Button on:click={() => expandDictionaryEntry = !expandDictionaryEntry}
                variant="fill-light"
                icon={expandDictionaryEntry ? mdiChevronUp : mdiChevronDown}
                class="p-2 absolute bottom-2 {expandDictionaryEntry ? 'right-2' : 'right-6'}" />
          {/if}
      </div>
      <EntryEditor on:change={() => {selectedEntry = selectedEntry; if (selectedEntry) selectedEntry.senses = selectedEntry.senses;}} entry={selectedEntry} />
    </div>

    <div class="h-full min-w-48 flex flex-col flex-grow self-start pl-8 border-l-2">
      <div class="overview border flex flex-col rounded-md">
        <a class="toc-item" href="#top">Entry: {firstVal(selectedEntry.lexemeForm)}</a>
        {#each selectedEntry.senses as sense, i}
          <a class="toc-item" href="#sense{i + 1}">Sense: {firstVal(sense.gloss)}</a>
          {#each sense.exampleSentences as example, j}
            <a class="toc-item" href="#example{i + 1}.{j + 1}">Example: {firstVal(example.sentence)}</a>
          {/each}
        {/each}
      </div>
    </div>
  {/if}
</div>

<style lang="postcss">
  .toc-item {
    padding: 4px 8px;
    &:hover {
      @apply bg-surface-300;
    }
  }

  :global(#entry :is(h2, h3)) {
    scroll-margin-top: 5.5rem;
  }

  :global(.hide-empty-fields .empty) {
    display: none !important;
  }

  .entry-list, .overview {
    max-height: calc(100vh - 32px);
    overflow: overflow-auto;
    position: sticky;
    top: 16px;
  }
</style>
