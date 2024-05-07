<script lang="ts">
  import { InfiniteScroll, ListItem, TextField, cls } from "svelte-ux";
  import type { IEntry } from "../mini-lcm";
  import { firstDefOrGlossVal, firstVal, headword } from "../utils";
  import { mdiMagnify } from "@mdi/js";
  import IndexCharacters from "./IndexCharacters.svelte";
  import type { Writable } from "svelte/store";
  import { getContext } from "svelte";

  export let entries: IEntry[] | undefined;
  export let search: string;

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
</script>

<div class="side-scroller flex flex-col gap-4">
  <div class="flex gap-3">
    <IndexCharacters />
    <div class="grow">
      <TextField
        bind:value={search}
        placeholder="Filter {entries?.length} entries..."
        icon={mdiMagnify} />
    </div>
  </div>
  <div bind:this={scrollContainerElem} class="border rounded-md overflow-auto">
    {#if !entries || entries.length == 0}
      <div class="p-4 text-center opacity-75">No entries found</div>
    {:else}
      <InfiniteScroll {perPage} items={entries} let:visibleItems>
        {#each visibleItems as entry (entry.id)}
          <ListItem
            title={headword(entry)}
            subheading={firstDefOrGlossVal(entry.senses[0]).padStart(1, '-')}
            on:click={() => ($selectedEntry = entry)}
            class={cls(
              'cursor-pointer',
              'hover:bg-surface-300',
              $selectedEntry == entry ? 'bg-surface-200 selected-entry' : ''
            )}
            noShadow
          />
        {/each}
      </InfiniteScroll>
    {/if}
  </div>
</div>
