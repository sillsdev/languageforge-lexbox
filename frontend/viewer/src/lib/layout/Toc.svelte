<script lang="ts">
  import type { IEntry } from "../mini-lcm";
  import { firstVal } from "../utils";

  export let entry: IEntry | undefined;
</script>

{#if entry}
  <div class="side-scroller border flex flex-col rounded-md">
    <a class="toc-item" href="#entry"><span class="opacity-80 mr-1">Entry:</span>{firstVal(entry.lexemeForm) ?? ''}</a>
    {#each entry.senses as sense, i (sense.id)}
      <a class="toc-item" href="#sense{i + 1}"><span class="opacity-80 mr-1">Sense:</span>{firstVal(sense.gloss) ?? ''}</a>
      {#each sense.exampleSentences as example, j (example.id)}
        <a class="toc-item" href="#example{i + 1}.{j + 1}"><span class="opacity-80 mr-1">Example:</span>{firstVal(example.sentence) ?? ''}</a>
      {/each}
    {/each}
  </div>
  {/if}

<style lang="postcss">
  .toc-item {
    padding: 4px 8px;
    &:hover {
      @apply bg-surface-300;
    }
  }
</style>
