<script lang="ts">
  import type { IEntry } from "../mini-lcm";
  import {
    customEntryFieldConfigs,
    customExampleFieldConfigs,
    customSenseFieldConfigs,
    entryFieldConfigs,
    exampleFieldConfigs,
    senseFieldConfigs,
  } from "../config-data";
  import EntityEditor from "./EntityEditor.svelte";

  export let entry: IEntry;
</script>

<EntityEditor
  entity={entry}
  fieldConfigs={entryFieldConfigs}
  customFieldConfigs={customEntryFieldConfigs}
  on:change
/>

{#each entry.senses as sense, i}
  <h2 class="text-lg" id="sense{i + 1}">Sense {i + 1}</h2>

  <EntityEditor
    entity={sense}
    fieldConfigs={senseFieldConfigs}
    customFieldConfigs={customSenseFieldConfigs}
    on:change
  />

  {#each sense.exampleSentences as example, j}
    <h3 class="font-bold" id="example{i + 1}.{j + 1}">Example {j + 1}</h3>

    <EntityEditor
      entity={example}
      fieldConfigs={exampleFieldConfigs}
      customFieldConfigs={customExampleFieldConfigs}
      on:change
    />
  {/each}
{/each}
