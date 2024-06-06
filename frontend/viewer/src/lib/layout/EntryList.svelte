<script lang="ts">
  import { Button, InfiniteScroll, ListItem, TextField } from "svelte-ux";
  import type { IEntry } from "../mini-lcm";
  import { firstDefOrGlossVal, headword } from "../utils";
  import { mdiArrowExpandLeft, mdiArrowExpandRight, mdiBookOpenVariantOutline, mdiBookSearchOutline, mdiFormatListText } from "@mdi/js";
  import IndexCharacters from "./IndexCharacters.svelte";
  import type { Writable } from "svelte/store";
  import { createEventDispatcher, getContext } from "svelte";
  import DictionaryEntry from "../DictionaryEntry.svelte";

  const dispatch = createEventDispatcher<{
    entrySelected: IEntry;
  }>();

  export let entries: IEntry[] | undefined;
  export let search: string;
  export let expand: boolean;

  $: {
    entries;
    // wait until the new entries have been rendered
    setTimeout(() => {
      const selected = scrollContainerElem?.querySelector('.selected-entry');
      selected?.scrollIntoView({block: 'center'});
    });
  }

  const selectedEntry = getContext<Writable<IEntry | undefined>>('selectedEntry');

  let scrollContainerElem: HTMLDivElement;
  $: {
    entries;
    if (scrollContainerElem) scrollContainerElem.scrollTop = 0;
  }

  const standardPageSize = 50;
  $: perPage = (!$selectedEntry || !entries)
    ? standardPageSize
    : Math.max(50, entries.indexOf($selectedEntry) + Math.ceil(standardPageSize / 2));

  function selectEntry(entry: IEntry) {
    $selectedEntry = entry;
    expand = false;
    dispatch('entrySelected', entry);
  }

  let dictionaryMode = false;
</script>

<div class="entry-list flex flex-col gap-4 w-full justify-self-center side-scroller">
  <div class="flex gap-3 w-full self-center">
    <IndexCharacters />
    <div class="grow">
      <TextField
        bind:value={search}
        placeholder="Filter entries..."
        icon={mdiBookSearchOutline} />
    </div>
    <Button icon={dictionaryMode ? mdiFormatListText : mdiBookOpenVariantOutline} variant="outline"
      class="text-field-sibling-button"
      rounded
      on:click={() => dictionaryMode = !dictionaryMode}>
    </Button>
    <div class="hidden md:contents">
      <Button icon={expand ? mdiArrowExpandLeft : mdiArrowExpandRight} variant="outline" iconOnly
        class="text-field-sibling-button"
        rounded
        on:click={() => expand = !expand}>
      </Button>
    </div>
  </div>
  <div class="border rounded-md overflow-hidden flex">
    <div class="overflow-auto w-full">
      {#if !entries || entries.length == 0}
        <div class="p-4 text-center opacity-75">No entries found</div>
      {:else}
        <InfiniteScroll {perPage} items={entries} let:visibleItems>
          {#each visibleItems as entry (entry.id)}
            <div class="entry" class:selected-entry={$selectedEntry == entry}>
              {#if dictionaryMode}
                <button class="p-2 w-full text-left"
                  on:click={() => selectEntry(entry)}>
                  <DictionaryEntry entry={entry} />
                </button>
              {:else}
                  <ListItem
                    title={headword(entry)}
                    subheading={firstDefOrGlossVal(entry.senses[0]).padStart(1, '-')}
                    on:click={() => selectEntry(entry)}
                    noShadow
                    class="!rounded-none"
                  />
              {/if}
            </div>
          {/each}
        </InfiniteScroll>
      {/if}
    </div>
  </div>
</div>

<style lang="postcss">
  .entry {
    cursor: pointer;

    &.selected-entry > :global(*) {
      @apply bg-surface-200;
    }

    &:hover > :global(*) {
      @apply bg-surface-300;
    }
  }
</style>
