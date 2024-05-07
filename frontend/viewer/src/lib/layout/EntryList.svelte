<script lang="ts">
  import { InfiniteScroll, ListItem, TextField, cls } from "svelte-ux";
  import type { IEntry } from "../mini-lcm";
  import { firstDefOrGlossVal, firstVal } from "../utils";
  import { mdiMagnify } from "@mdi/js";
  import IndexCharacters from "./IndexCharacters.svelte";

  export let entries: IEntry[] | undefined;
  export let selectedEntry: IEntry | undefined;
  $: {
    if (!selectedEntry) selectedEntry = entries?.[0];
    else if (entries && !entries.includes(selectedEntry)) selectedEntry = entries[0];
  }
  export let search: string;

  let scrollContainerElem: HTMLDivElement;
  $: {
    entries;
    if (scrollContainerElem) scrollContainerElem.scrollTop = 0;
  }
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
      <InfiniteScroll perPage={50} items={entries} let:visibleItems>
        {#each visibleItems as entry (entry.id)}
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
      </InfiniteScroll>
    {/if}
  </div>
</div>
