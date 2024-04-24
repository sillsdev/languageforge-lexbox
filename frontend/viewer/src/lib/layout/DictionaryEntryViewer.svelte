<script lang="ts">
  import type { IEntry } from "../mini-lcm";
  import DictionaryEntry from '../DictionaryEntry.svelte';
  import { Button } from "svelte-ux";
  import { mdiChevronDown, mdiChevronUp } from "@mdi/js";

  export let entry: IEntry;

  let dictionaryEntryLines: number;
  let expandDictionaryEntry = true;
</script>

<div class="relative">
  <div class="bg-neutral text-surface-content overflow-auto rounded-sm shadow shadow-neutral" class:max-h-14={!expandDictionaryEntry}>
    <div class="px-3 py-2 text-sm">
      <DictionaryEntry entry={entry} bind:lines={dictionaryEntryLines} />
    </div>
  </div>
  {#if dictionaryEntryLines > 2}
    <Button on:click={() => expandDictionaryEntry = !expandDictionaryEntry}
        variant="fill-light"
        icon={expandDictionaryEntry ? mdiChevronUp : mdiChevronDown}
        iconOnly
        size="sm"
        class="p-2 absolute bottom-2 {expandDictionaryEntry ? 'right-2' : 'right-6'}" />
    {/if}
</div>
