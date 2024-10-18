<script lang="ts">
  import type {IEntry, IExampleSentence, ISense} from '../../mini-lcm';
  import EntityEditor from './EntityEditor.svelte';
  import {createEventDispatcher, getContext} from 'svelte';
  import type { Readable } from 'svelte/store';
  import type { LexboxFeatures } from '../../config-types';
  import {mdiPlus, mdiTrashCanOutline} from '@mdi/js';
  import { Button, portal } from 'svelte-ux';
  import EntityListItemActions from '../EntityListItemActions.svelte';
  import {defaultExampleSentence, defaultSense, emptyId, firstDefOrGlossVal, firstSentenceOrTranslationVal} from '../../utils';
  import HistoryView from '../../history/HistoryView.svelte';
  import SenseEditor from './SenseEditor.svelte';
  import ExampleEditor from './ExampleEditor.svelte';
  import MultiFieldEditor from '../field-editors/MultiFieldEditor.svelte';
  import {objectTemplateAreas, useCurrentView} from '../../services/view-service';
  import ComplexFormComponents from '../field-editors/ComplexFormComponents.svelte';
  import ComplexForms from '../field-editors/ComplexForms.svelte';
  import ComplexFormTypes from '../field-editors/ComplexFormTypes.svelte';

  const dispatch = createEventDispatcher<{
    change: { entry: IEntry, sense?: ISense, example?: IExampleSentence};
    delete: { entry: IEntry, sense?: ISense, example?: IExampleSentence};
  }>();

  export let entry: IEntry;

  function addSense() {
    const sense = defaultSense();
    highlightedEntity = sense;
    entry.senses = [...entry.senses, sense];
  }

  function addExample(sense: ISense) {
    const sentence = defaultExampleSentence();
    highlightedEntity = sentence;
    sense.exampleSentences = [...sense.exampleSentences, sentence];
    entry = entry; // examples counts are not updated without this
  }
  function deleteEntry() {
    dispatch('delete', {entry});
  }

  function deleteSense(sense: ISense) {
    entry.senses = entry.senses.filter(s => s !== sense);
    dispatch('delete', {entry, sense});
  }
  function moveSense(sense: ISense, i: number) {
    entry.senses.splice(entry.senses.indexOf(sense), 1);
    entry.senses.splice(i, 0, sense);
    dispatch('change', {entry, sense});
    highlightedEntity = sense;
  }
  function deleteExample(sense: ISense, example: IExampleSentence) {
    sense.exampleSentences = sense.exampleSentences.filter(e => e !== example);
    dispatch('delete', {entry, sense, example});
    entry = entry; // examples are not updated without this
  }
  function moveExample(sense: ISense, example: IExampleSentence, i: number) {
    sense.exampleSentences.splice(sense.exampleSentences.indexOf(example), 1);
    sense.exampleSentences.splice(i, 0, example);
    dispatch('change', {entry, sense, example});
    highlightedEntity = example;
    entry = entry; // examples are not updated without this
  }
  export let modalMode = false;
  export let readonly = false;

  let editorElem: HTMLDivElement | undefined;
  let highlightedEntity: IExampleSentence | ISense | undefined;
  let highlightTimeout: ReturnType<typeof setTimeout>;

  $: {
    if (highlightedEntity) {
      clearTimeout(highlightTimeout);
      highlightTimeout = setTimeout(() => highlightedEntity = undefined, 3000);
      // wait for rendering
      setTimeout(() => {
        const newEntityElem = editorElem?.querySelector('.highlight');
        if (newEntityElem) {
          const _isBottomInViewport = isBottomInView(newEntityElem);
          const _isTopInViewport = isTopInView(newEntityElem);
          if (!_isBottomInViewport && !_isTopInViewport)
            newEntityElem?.scrollIntoView({block: 'start', behavior: 'smooth'});
          else if (!_isTopInViewport)
            newEntityElem?.scrollIntoView({block: 'nearest', behavior: 'smooth'});
          else if (!_isBottomInViewport)
            newEntityElem?.scrollIntoView({block: 'center', behavior: 'smooth'});
        }
      });
    }
  }

  function isBottomInView(element: Element): boolean {
    const elementRect = element.getBoundingClientRect();
    const viewportHeight = window.innerHeight || document.documentElement.clientHeight;
    // + 40 = simply way to make the check forgiving for if e.g. there's a sticky footer
    return (elementRect.bottom + 40) <= viewportHeight;
  }

  function isTopInView(element: Element): boolean {
    const elementRect = element.getBoundingClientRect();
    return elementRect.top >= 0;
  }

  const features = getContext<Readable<LexboxFeatures>>('features');
  const entryActionsPortal = getContext<Readable<{target: HTMLDivElement, collapsed: boolean}>>('entryActionsPortal');
  const currentView = useCurrentView();
</script>

<div bind:this={editorElem} class="editor-grid">
  <div class="grid-layer" style:grid-template-areas={`${objectTemplateAreas($currentView, entry)}`}>
    <MultiFieldEditor on:change={() => dispatch('change', {entry})}
                      bind:value={entry.lexemeForm}
                      {readonly}
                      id="lexemeForm"
                      wsType="vernacular"/>

    <MultiFieldEditor on:change={() => dispatch('change', {entry})}
                      bind:value={entry.citationForm}
                      {readonly}
                      id="citationForm"
                      wsType="vernacular"/>

    <ComplexForms on:change={() => dispatch('change', {entry})}
                  bind:value={entry.complexForms}
                  {readonly}
                  {entry}
                  id="complexForms" />

    <ComplexFormTypes on:change={() => dispatch('change', {entry})}
                  bind:value={entry.complexFormTypes}
                  {readonly}
                  id="complexFormTypes" />

    <ComplexFormComponents  on:change={() => dispatch('change', {entry})}
                            bind:value={entry.components}
                            {readonly}
                            {entry}
                            id="components" />

    <MultiFieldEditor on:change={() => dispatch('change', {entry})}
                      bind:value={entry.literalMeaning}
                      {readonly}
                      id="literalMeaning"
                      wsType="vernacular"/>
    <MultiFieldEditor on:change={() => dispatch('change', {entry})}
                      bind:value={entry.note}
                      {readonly}
                      id="note"
                      wsType="analysis"/>
    <EntityEditor
      entity={entry}
      {readonly}
      customFieldConfigs={[]}
      on:change={() => dispatch('change', {entry})}
    />
  </div>

  {#each entry.senses as sense, i (sense.id)}
    <div class="grid-layer" class:highlight={sense === highlightedEntity}>
      <div id="sense{i + 1}"></div> <!-- shouldn't be in the sticky header -->
      <div class="col-span-full flex items-center gap-4 py-4 sticky top-[-1px] bg-surface-100 z-[1]">
        <h2 class="text-lg text-surface-content">Sense {i + 1}</h2>
        <hr class="grow border-t-4">
        {#if !readonly}
          <EntityListItemActions {i} items={entry.senses.map(firstDefOrGlossVal)}
            on:move={(e) => moveSense(sense, e.detail)}
            on:delete={() => deleteSense(sense)} id={sense.id} />
        {/if}
      </div>

      <SenseEditor {sense} {readonly} on:change={() => dispatch('change', {entry, sense})}/>

      <div class="grid-layer border-l border-dashed pl-4 space-y-4 rounded-lg">
        {#each sense.exampleSentences as example, j (example.id)}
          <div class="grid-layer" class:highlight={example === highlightedEntity}>
            <div id="example{i + 1}-{j + 1}"></div> <!-- shouldn't be in the sticky header -->
            <div class="col-span-full flex items-center gap-4 mb-4">
              <h3 class="text-surface-content">Example {j + 1}</h3>
              <!--
                <hr class="grow">
                collapse/expand toggle
              -->
              <hr class="grow">
              {#if !readonly}
              <EntityListItemActions i={j}
                                     items={sense.exampleSentences.map(firstSentenceOrTranslationVal)}
                  on:move={(e) => moveExample(sense, example, e.detail)}
                                     on:delete={() => deleteExample(sense, example)}
                                     id={example.id}
              />
              {/if}
            </div>

            <ExampleEditor
              {example}
              {readonly}
                on:change={() => dispatch('change', {entry, sense, example})}
              />
          </div>
        {/each}
      </div>
      {#if !readonly}
        <div class="col-span-full flex justify-end mt-4">
          <Button on:click={() => addExample(sense)} icon={mdiPlus} variant="fill-light" color="success" size="sm">Add Example</Button>
        </div>
      {/if}
    </div>
  {/each}
  {#if !readonly}
    <hr class="col-span-full grow border-t-4 my-4">
    <div class="col-span-full flex justify-end">
      <Button on:click={addSense} icon={mdiPlus} variant="fill-light" color="success" size="sm">Add Sense</Button>
    </div>
  {/if}
</div>

{#if !modalMode && !readonly}
  <div class="hidden">
    <div class="contents" use:portal={{ target: $entryActionsPortal.target, enabled: !!$entryActionsPortal.target}}>
      <Button on:click={addSense} icon={mdiPlus} variant="fill-light" color="success" size="sm">
        <div class="hidden" class:sm:contents={!$entryActionsPortal.collapsed}>
          Add Sense
        </div>
      </Button>
      <Button on:click={deleteEntry} icon={mdiTrashCanOutline} variant="fill-light" color="danger" size="sm">
        <div class="hidden" class:sm:contents={!$entryActionsPortal.collapsed}>
          Delete Entry
        </div>
      </Button>
      {#if $features.history}
        <HistoryView id={entry.id} small={$entryActionsPortal.collapsed} />
      {/if}
    </div>
  </div>
{/if}

<style lang="postcss">
  .highlight {
    & :is(h2, h3) {
      @apply text-info-500;
    }

    & hr {
      @apply border-info-500;
    }
  }
</style>
