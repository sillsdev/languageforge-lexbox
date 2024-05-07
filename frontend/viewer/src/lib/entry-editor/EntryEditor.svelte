<script lang="ts">
  import type {IEntry, IExampleSentence, ISense} from '../mini-lcm';
  import EntityEditor from './EntityEditor.svelte';
  import {createEventDispatcher, getContext} from 'svelte';
  import type { Readable } from 'svelte/store';
  import type { ViewConfig } from '../config-types';
  import { mdiPlus } from '@mdi/js';
  import { Button, portal } from 'svelte-ux';
  import EntityListItemActions from './EntityListItemActions.svelte';
  import {defaultExampleSentence, defaultSense} from '../utils';

  const dispatch = createEventDispatcher<{
    change: { entry: IEntry, sense?: ISense, example?: IExampleSentence};
  }>();

  export let entry: IEntry;

  function addSense() {
    const sense = defaultSense();
    newEntity = sense;
    entry.senses = [...entry.senses, sense];
  }

  function addExample(sense: ISense) {
    const sentence = defaultExampleSentence();
    newEntity = sentence;
    sense.exampleSentences = [...sense.exampleSentences, sentence];
  }
  export let modalMode = false;

  let newEntity: IExampleSentence | ISense | undefined;
  let editorElem: HTMLDivElement | undefined;
  let newEntityTimeout: ReturnType<typeof setTimeout>;

  $: {
    if (newEntity) {
      clearTimeout(newEntityTimeout);
      newEntityTimeout = setTimeout(() => newEntity = undefined, 3000);
      // wait for rendering
      setTimeout(() => {
        const newEntityElem = editorElem?.querySelector('.new-entity');
        if (newEntityElem) {
          if (!isBottomInViewport(newEntityElem))
            newEntityElem?.scrollIntoView({block: 'center', behavior: 'smooth'});
        }
      });
    }
  }

  function isBottomInViewport(element: Element): boolean {
    const elementRect = element.getBoundingClientRect();
    const viewportHeight = window.innerHeight || document.documentElement.clientHeight;
    return elementRect.bottom <= viewportHeight;
}

  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');
  const entryActionsPortal = getContext<Readable<HTMLElement>>('entryActionsPortal');
</script>

<div bind:this={editorElem} class="grid" style="grid-template-columns: 170px 40px 1fr">
  <EntityEditor
    entity={entry}
    fieldConfigs={Object.values($viewConfig.activeView.entry ?? [])}
    customFieldConfigs={Object.values($viewConfig.activeView?.customEntry ?? [])}
    on:change={() => dispatch('change', {entry})}
  />

  {#each entry.senses as sense, i (sense.id)}
    <div class="grid-layer" class:new-entity={sense.id === newEntity?.id}>
      <div class="col-span-full flex items-center gap-4 my-4 sticky top-[-1px] bg-surface-100 z-1">
        <h2 class="text-lg text-surface-content" id="sense{i + 1}">Sense {i + 1}</h2>
        <hr class="grow border-t-4">
        {#if !$viewConfig.readonly}
          <EntityListItemActions {i} count={entry.senses.length} />
        {/if}
      </div>

      <div class="grid-layer">
        <EntityEditor
          entity={sense}
          fieldConfigs={Object.values($viewConfig.activeView?.sense ?? [])}
          customFieldConfigs={Object.values($viewConfig.activeView?.customSense ?? [])}
          on:change={() => dispatch('change', {entry, sense})}
        />
      </div>

      {#each sense.exampleSentences as example, j (example.id)}
        <div class="grid-layer" class:new-entity={example.id === newEntity?.id}>
          <div class="col-span-full flex items-center gap-4 my-4">
            <h3 class="text-surface-content" id="example{i + 1}.{j + 1}">Example {j + 1}</h3>
            <hr class="grow">
            {#if !$viewConfig.readonly}
              <EntityListItemActions i={j} count={entry.senses.length} />
            {/if}
          </div>

          <div class="grid-layer">
            <EntityEditor
              entity={example}
              fieldConfigs={Object.values($viewConfig.activeView?.example ?? [])}
              customFieldConfigs={Object.values($viewConfig.activeView?.customExample ?? [])}
              on:change={() => dispatch('change', {entry, sense, example})}
            />
          </div>
        </div>
      {/each}
      {#if !$viewConfig.readonly}
        <div class="col-span-full flex justify-end mt-4">
          <Button on:click={() => addExample(sense)} icon={mdiPlus} variant="fill-light" color="success" size="sm">Add Example</Button>
        </div>
      {/if}
    </div>
  {/each}
  {#if !$viewConfig.readonly}
    <hr class="col-span-full grow border-t-4 my-4">
    <div class="col-span-full flex justify-end">
      <Button on:click={addSense} icon={mdiPlus} variant="fill-light" color="success" size="sm">Add Sense</Button>
    </div>
  {/if}
</div>

{#if !modalMode}
  <div class="contents" use:portal={{ target: $entryActionsPortal}}>
    <Button on:click={addSense} icon={mdiPlus} variant="fill-light" color="success" size="sm">Add Sense</Button>
  </div>
{/if}

<style lang="postcss">
  .new-entity {
    & :is(h2, h3) {
      @apply text-info-500;
    }

    & hr {
      @apply border-info-500;
    }
  }

  .grid-layer {
    display: grid;
    grid-template-columns: subgrid;
    @apply col-span-full;
  }
</style>
