<script lang="ts">
  import { ListItem, cls } from "svelte-ux";
  import type { IEntry } from "./mini-lcm";
  import { firstDefVal, firstVal } from "./utils";
  import EntryEditor from "./entry-editor/EntryEditor.svelte";

  export let entries: IEntry[];

  let selectedEntry: IEntry | undefined = entries[0];
</script>

<div class="grid grid-cols-subgrid col-span-3 flex-grow gap-8">
  <div class="min-w-48 pr-8 border-r-2">
    <div class="grid border">
      {#each entries as entry}
        <ListItem
          title={firstVal(entry.lexemeForm)}
          subheading={firstDefVal(entry)}
          on:click={() => (selectedEntry = entry)}
          class={cls(
            "cursor-pointer",
            "hover:bg-accent-50",
            selectedEntry == entry ? "bg-accent-50" : ""
          )}
          noShadow
        />
      {/each}
    </div>
  </div>

  {#if selectedEntry}
    <div id="entry" class="grid self-start pr-8 border-r-2" style="grid-template-columns: 170px 40px 1fr">
      <EntryEditor on:change={() => {selectedEntry = selectedEntry; if (selectedEntry) selectedEntry.senses = selectedEntry.senses;}} entry={selectedEntry} />
    </div>

    <div class="min-w-48 flex flex-col flex-grow max-h-[calc(100vh - 30px) sticky top-[15px] self-start">
      <div class="border flex flex-col">
        <a class="toc-item" href="#entry">Entry: {firstVal(selectedEntry.lexemeForm)}</a>
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
      background-color: #eee;
    }
  }
</style>
