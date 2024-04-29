<script lang="ts">
  import type {IEntry, IExampleSentence, ISense, MultiString} from '../mini-lcm';
  import {
    type views,
  } from '../config-data';
  import EntityEditor from './EntityEditor.svelte';
  import {createEventDispatcher, getContext} from 'svelte';
  import type { Readable } from 'svelte/store';
  import type { ViewConfig } from '../config-types';
  const dispatch = createEventDispatcher<{
    change: { entry: IEntry, sense?: ISense, example?: IExampleSentence};
  }>();

  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');

  export let entry: IEntry;
</script>

<EntityEditor
  entity={entry}
  fieldConfigs={Object.values($viewConfig.activeView.entry ?? [])}
  customFieldConfigs={Object.values($viewConfig.activeView?.customEntry ?? [])}
  on:change={() => dispatch('change', {entry})}
/>

{#each entry.senses as sense, i (sense.id)}
  <div class="col-span-full flex items-center gap-4 my-4">
    <h2 class="text-lg text-surface-content" id="sense{i + 1}">Sense {i + 1}</h2>
    <hr class="grow border-t-4">
  </div>

  <EntityEditor
    entity={sense}
    fieldConfigs={Object.values($viewConfig.activeView?.sense ?? [])}
    customFieldConfigs={Object.values($viewConfig.activeView?.customSense ?? [])}
    on:change={() => dispatch('change', {entry, sense})}
  />

  {#each sense.exampleSentences as example, j (example.id)}
    <div class="col-span-full flex items-center gap-4 my-4">
      <h3 class="text-surface-content" id="example{i + 1}.{j + 1}">Example {j + 1}</h3>
      <hr class="grow">
    </div>

    <EntityEditor
      entity={example}
      fieldConfigs={Object.values($viewConfig.activeView?.example ?? [])}
      customFieldConfigs={Object.values($viewConfig.activeView?.customExample ?? [])}
      on:change={() => dispatch('change', {entry, sense, example})}
    />
  {/each}
{/each}
