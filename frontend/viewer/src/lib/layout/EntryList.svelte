<script lang="ts">
  import { InfiniteScroll, ListItem, TextField, cls } from "svelte-ux";
  import type { IEntry } from "../mini-lcm";
  import { firstDefOrGlossVal, firstVal } from "../utils";
  import { mdiMagnify } from "@mdi/js";

  export let entries: IEntry[] | undefined;
  export let selectedEntry: IEntry | undefined;
  $: {
    if (!selectedEntry) selectedEntry = entries?.[0];
    else if (entries && !entries.includes(selectedEntry)) selectedEntry = entries[0];
  }
  export let search: string;
</script>

<div class="side-scroller flex flex-col gap-4">
  <div class="flex gap-4 items-end">
    <div class="grow">
      <TextField
        bind:value={search}
        placeholder="Filter {entries?.length} entries..."
        icon={mdiMagnify} />
    </div>
  </div>
  <div class="border rounded-md overflow-auto">
    {#if !entries || entries.length == 0}
      <div class="p-4 text-center opacity-75">No entries found</div>
    {:else}
      <InfiniteScroll perPage={100} items={entries} let:visibleItems>
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
