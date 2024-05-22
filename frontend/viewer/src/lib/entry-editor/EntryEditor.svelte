<script lang="ts">
  import type { IEntry } from '../mini-lcm';
  import {
    type views,
  } from '../config-data';
  import EntityEditor from './EntityEditor.svelte';
  import { getContext } from 'svelte';
  import type { Readable } from 'svelte/store';

  // eslint-disable-next-line @typescript-eslint/no-unnecessary-type-assertion
  const activeView = getContext('activeView') as Readable<typeof views[number]['value']>;

  export let entry: IEntry;
</script>

<EntityEditor
  entity={entry}
  fieldConfigs={Object.values($activeView?.entry ?? [])}
  customFieldConfigs={Object.values($activeView?.customEntry ?? [])}
  on:change
/>

{#each entry.senses as sense, i}
  <div class="col-span-full flex items-center gap-4 my-4">
    <h2 class="text-lg text-surface-content" id="sense{i + 1}">Sense {i + 1}</h2>
    <hr class="grow border-t-4">
  </div>

  <EntityEditor
    entity={sense}
    fieldConfigs={Object.values($activeView?.sense ?? [])}
    customFieldConfigs={Object.values($activeView?.customSense ?? [])}
    on:change
  />

  {#each sense.exampleSentences as example, j}
    <div class="col-span-full flex items-center gap-4 my-4">
      <h3 class="text-surface-content" id="example{i + 1}.{j + 1}">Example {j + 1}</h3>
      <hr class="grow">
    </div>

    <EntityEditor
      entity={example}
      fieldConfigs={Object.values($activeView?.example ?? [])}
      customFieldConfigs={Object.values($activeView?.customExample ?? [])}
      on:change
    />
  {/each}
{/each}
