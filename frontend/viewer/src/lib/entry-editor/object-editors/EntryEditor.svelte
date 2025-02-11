<script lang="ts">
  import type {IEntry, IExampleSentence, ISense} from '$lib/dotnet-types';
  import {useDialogService} from '$lib/entry-editor/dialog-service';
  import {fieldName} from '$lib/i18n';
  import Scotty from '$lib/layout/Scotty.svelte';
  import {useFeatures} from '$lib/services/feature-service';
  import {objectTemplateAreas, useCurrentView} from '$lib/views/view-service';
  import {defaultExampleSentence, defaultSense} from '$lib/utils';
  import {useWritingSystemService} from '$lib/writing-system-service';
  import {mdiHistory, mdiPlus, mdiTrashCanOutline} from '@mdi/js';
  import {createEventDispatcher} from 'svelte';
  import {Button, MenuItem} from 'svelte-ux';
  import HistoryView from '../../history/HistoryView.svelte';
  import EntityListItemActions from '../EntityListItemActions.svelte';
  import ComplexFormComponents from '../field-editors/ComplexFormComponents.svelte';
  import ComplexForms from '../field-editors/ComplexForms.svelte';
  import ComplexFormTypes from '../field-editors/ComplexFormTypes.svelte';
  import MultiFieldEditor from '../field-editors/MultiFieldEditor.svelte';
  import AddSenseFab from './AddSenseFab.svelte';
  import EntityEditor from './EntityEditor.svelte';
  import ExampleEditor from './ExampleEditor.svelte';
  import SenseEditor from './SenseEditor.svelte';

  const dialogService = useDialogService();
  const writingSystemService = useWritingSystemService();
  const dispatch = createEventDispatcher<{
    change: { entry: IEntry, sense?: ISense, example?: IExampleSentence};
    delete: { entry: IEntry, sense?: ISense, example?: IExampleSentence};
  }>();

  export let entry: IEntry;
  //used to not try to delete an object which has not been created yet
  let newSenses: ISense[] = [];
  let newExamples: IExampleSentence[] = [];

  function addSense() {
    const sense = defaultSense(entry.id);
    highlightedEntity = sense;
    entry.senses = [...entry.senses, sense];
    newSenses = [...newSenses, sense];
  }

  function addExample(sense: ISense) {
    const sentence = defaultExampleSentence(sense.id);
    highlightedEntity = sentence;
    sense.exampleSentences = [...sense.exampleSentences, sentence];
    entry = entry; // examples counts are not updated without this
    newExamples = [...newExamples, sentence];
  }
  async function deleteEntry() {
    if (!await dialogService.promptDelete('Entry')) return;
    dispatch('delete', {entry});
  }

  async function deleteSense(sense: ISense) {
    if (newSenses.some(s => s.id === sense.id)) {
      newSenses = newSenses.filter(s => s.id !== sense.id);
      entry.senses = entry.senses.filter(s => s.id !== sense.id);
      return;
    }
    if (!await dialogService.promptDelete('Sense')) return;
    entry.senses = entry.senses.filter(s => s.id !== sense.id);
    dispatch('delete', {entry, sense});
  }
  function moveSense(sense: ISense, i: number) {
    entry.senses.splice(entry.senses.indexOf(sense), 1);
    entry.senses.splice(i, 0, sense);
    onSenseChange(sense);
    highlightedEntity = sense;
  }

  async function deleteExample(sense: ISense, example: IExampleSentence) {
    if (newExamples.some(e => e.id === example.id)) {
      newExamples = newExamples.filter(e => e.id !== example.id);
      sense.exampleSentences = sense.exampleSentences.filter(e => e.id !== example.id);
      entry = entry; // examples are not updated without this
      return;
    }
    if (!await dialogService.promptDelete('Example sentence')) return;
    sense.exampleSentences = sense.exampleSentences.filter(e => e.id !== example.id);
    dispatch('delete', {entry, sense, example});
    entry = entry; // examples are not updated without this
  }
  function moveExample(sense: ISense, example: IExampleSentence, i: number) {
    sense.exampleSentences.splice(sense.exampleSentences.indexOf(example), 1);
    sense.exampleSentences.splice(i, 0, example);
    onExampleChange(sense, example);
    highlightedEntity = example;
    entry = entry; // examples are not updated without this
  }

  function onSenseChange(sense: ISense) {
    newSenses = newSenses.filter(s => s.id !== sense.id);
    dispatch('change', {entry, sense});
  }
  function onExampleChange(sense: ISense, example: IExampleSentence) {
    newExamples = newExamples.filter(e => e.id !== example.id);
    dispatch('change', {entry, sense, example});
  }
  export let modalMode = false;
  export let readonly = false;
  export let canAddSense = true;
  export let canAddExample = true;

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
    const viewportHeight = window.innerHeight || document.documentElement.clientHeight;
    return elementRect.top <= viewportHeight;
  }

  const features = useFeatures();
  const currentView = useCurrentView();

  let showHistoryView = false;
</script>

<div bind:this={editorElem} class="editor-grid">
  <div class="grid-layer" style:grid-template-areas={`${objectTemplateAreas($currentView, entry)}`}>
    <MultiFieldEditor on:change={() => dispatch('change', {entry})}
                      bind:value={entry.lexemeForm}
                      {readonly}
                      autofocus={modalMode}
                      id="lexemeForm"
                      wsType="vernacular"/>

    <MultiFieldEditor on:change={() => dispatch('change', {entry})}
                      bind:value={entry.citationForm}
                      {readonly}
                      id="citationForm"
                      wsType="vernacular"/>

    {#if !modalMode}

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

    {/if}

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
      {readonly}
      customFieldConfigs={[]}
      on:change={() => dispatch('change', {entry})}
    />
  </div>

  {#each entry.senses as sense, i (sense.id)}
    <div class="grid-layer" class:highlight={sense === highlightedEntity}>
      <div id="sense{i + 1}"></div> <!-- shouldn't be in the sticky header -->
      <div class="col-span-full flex items-center py-2 my-2 sticky top-[-1px] sm-view:top-12 bg-surface-100/70 z-[1]">
        <h2 class="text-lg text-surface-content mr-4">{fieldName({id: 'sense'}, $currentView.i18nKey)} {i + 1}</h2>
        <hr class="grow border-t-2">
        <div class="bg-surface-100">
          <EntityListItemActions {i} items={entry.senses.map(sense => writingSystemService.firstDefOrGlossVal(sense))}
              {readonly}
              on:move={(e) => moveSense(sense, e.detail)}
              on:delete={() => deleteSense(sense)} id={sense.id} />
        </div>
      </div>

      <SenseEditor {sense} {readonly} on:change={() => onSenseChange(sense)}/>

      {#if sense.exampleSentences.length}
        <div class="grid-layer border-l border-dashed pl-4 mt-4 space-y-4 rounded-lg">
          {#each sense.exampleSentences as example, j (example.id)}
            <div class="grid-layer" class:highlight={example === highlightedEntity}>
              <div id="example{i + 1}-{j + 1}"></div> <!-- shouldn't be in the sticky header -->
              <div class="col-span-full flex items-center mb-4">
                <h3 class="text-surface-content mr-4">Example {j + 1}</h3>
                <!--
                  <hr class="grow">
                  collapse/expand toggle
                -->
                <hr class="grow">
                <EntityListItemActions i={j} {readonly}
                                      items={sense.exampleSentences.map(example => writingSystemService.firstSentenceOrTranslationVal(example))}
                                      on:move={(e) => moveExample(sense, example, e.detail)}
                                      on:delete={() => deleteExample(sense, example)}
                                      id={example.id}
                />
              </div>

              <ExampleEditor
                {example}
                {readonly}
                on:change={() => onExampleChange(sense, example)}
                />
            </div>
          {/each}
        </div>
      {/if}
      {#if !readonly && canAddExample}
        <div class="col-span-full flex justify-end mt-4">
          <Button on:click={() => addExample(sense)} icon={mdiPlus} variant="fill-light" color="success" size="sm">Add Example</Button>
        </div>
      {/if}
    </div>
  {/each}
  {#if !readonly && canAddSense}
    <hr class="col-span-full grow border-t-4 my-4">
    <div class="lg-view:hidden flex col-span-full justify-end sticky bottom-3 right-3 z-[2]" class:hidden={modalMode}>
      <!-- sticky isn't working in the new entry dialog. I think that's fine/good. -->
      <AddSenseFab on:click={addSense} />
    </div>
    <div class="col-span-full flex justify-end" class:sm-view:hidden={!modalMode}>
      <Button on:click={addSense} icon={mdiPlus} variant="fill-light" color="success" size="sm">Add {fieldName({id: 'sense'}, $currentView.i18nKey)}</Button>
    </div>
  {/if}
</div>

{#if !modalMode}
{@const willRenderAnyButtons = $features.history || !readonly}
  {#if willRenderAnyButtons}
  <div class="hidden">
    <Scotty beamMeTo="right-toolbar" let:projectViewState>
      {#if !readonly}
        <Button on:click={addSense} title="Add sense" icon={mdiPlus} variant="fill-light" color="success" size="sm">
          <div class="sm-form:hidden" class:hidden={projectViewState.rightToolbarCollapsed}>
            Add {fieldName({id: 'sense'}, $currentView.i18nKey)}
          </div>
        </Button>
        <Button on:click={deleteEntry} title="Delete entry" icon={mdiTrashCanOutline} variant="fill-light" color="danger" size="sm">
          <div class="sm-form:hidden" class:hidden={projectViewState.rightToolbarCollapsed}>
            Delete {fieldName({id: 'entry'}, $currentView.i18nKey)}
          </div>
        </Button>
      {/if}
      {#if $features.history}
        <Button on:click={() => showHistoryView = true} title="View entry level history" icon={mdiHistory} variant="fill-light" color="info" size="sm">
          <div class="sm-form:hidden" class:hidden={projectViewState.rightToolbarCollapsed}>
            History
          </div>
        </Button>
      {/if}
    </Scotty>
    <Scotty beamMeTo="app-bar-menu" let:projectViewState>
      {#if projectViewState.userPickedEntry}
        <div class="lg-view:hidden">
          {#if !readonly}
            <MenuItem on:click={deleteEntry} icon={mdiTrashCanOutline}>
              Delete {fieldName({id: 'entry'}, $currentView.i18nKey)}
            </MenuItem>
          {/if}
          {#if $features.history}
            <MenuItem on:click={() => showHistoryView = true} icon={mdiHistory}>
              History
            </MenuItem>
          {/if}
          <hr class="border-surface-300" />
        </div>
      {/if}
    </Scotty>
  </div>

  {/if}
  {#if $features.history}
    <HistoryView id={entry.id} bind:open={showHistoryView} />
  {/if}
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
