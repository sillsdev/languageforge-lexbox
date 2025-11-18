<script lang="ts" module>
  export type Props = {
    entry: IEntry;
    readonly?: boolean;
    autofocus?: boolean;
    modalMode?: boolean;
    canAddSense?: boolean;
    canAddExample?: boolean;
    onchange?: (changed: { entry: IEntry, sense?: ISense, example?: IExampleSentence }) => void;
    ondelete?: (deleted: { entry: IEntry, sense?: ISense, example?: IExampleSentence }) => void;
    ref?: HTMLElement | null;
  };
</script>
<script lang="ts">
  import type {IEntry, IExampleSentence, ISense} from '$lib/dotnet-types';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {useCurrentView} from '$lib/views/view-service';
  import {cn, defaultExampleSentence, defaultSense} from '$lib/utils';
  import {useWritingSystemService} from '$project/data';
  import EntityListItemActions from '../EntityListItemActions.svelte';
  import AddSenseFab from './AddSenseFab.svelte';
  import ExampleEditorPrimitive from './ExampleEditorPrimitive.svelte';
  import SenseEditorPrimitive from './SenseEditorPrimitive.svelte';
  import * as Editor from '$lib/components/editor';
  import EntryEditorPrimitive from './EntryEditorPrimitive.svelte';
  import {t} from 'svelte-i18n-lingui';
  import {pt} from '$lib/views/view-text';
  import {Button} from '$lib/components/ui/button';
  import {watch} from 'runed';
  import FabContainer from '$lib/components/fab/fab-container.svelte';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {findFirstTabbable} from '$lib/utils/tabbable';
  import DevContent from '$lib/layout/DevContent.svelte';
  import ObjectHeader from './ObjectHeader.svelte';
  import AddSenseButton from './AddSenseButton.svelte';

  let {
    entry = $bindable(),
    ref = $bindable(null),
    readonly = false,
    autofocus = false,
    modalMode = false,
    canAddSense = true,
    canAddExample = true,
    onchange,
    ondelete,
  }: Props = $props();

  const dialogService = useDialogsService();
  const writingSystemService = useWritingSystemService();

  //used to not try to delete an object which has not been created yet
  let newSenses: ISense[] = [];
  let newExamples: IExampleSentence[] = [];

  function addSense() {
    const sense = defaultSense(entry.id);
    highlighted = { entity: sense, autofocus: true };
    entry.senses = [...entry.senses, sense];
    newSenses = [...newSenses, sense];
  }

  function addExample(sense: ISense) {
    const sentence = defaultExampleSentence(sense.id);
    highlighted = { entity: sentence, autofocus: true };
    sense.exampleSentences = [...sense.exampleSentences, sentence];
    entry = entry; // examples counts are not updated without this
    newExamples = [...newExamples, sentence];
  }

  async function deleteSense(sense: ISense) {
    if (newSenses.some(s => s.id === sense.id)) {
      newSenses = newSenses.filter(s => s.id !== sense.id);
      entry.senses = entry.senses.filter(s => s.id !== sense.id);
      return;
    }
    if (!await dialogService.promptDelete(pt($t`Sense`, $t`Meaning`, $currentView), writingSystemService.firstGloss(sense))) return;
    entry.senses = entry.senses.filter(s => s.id !== sense.id);
    ondelete?.({entry, sense});
  }
  function moveSense(sense: ISense, i: number) {
    entry.senses.splice(entry.senses.indexOf(sense), 1);
    entry.senses.splice(i, 0, sense);
    onSenseChange(sense);
    highlighted = { entity: sense };
  }

  async function deleteExample(sense: ISense, example: IExampleSentence) {
    if (newExamples.some(e => e.id === example.id)) {
      newExamples = newExamples.filter(e => e.id !== example.id);
      sense.exampleSentences = sense.exampleSentences.filter(e => e.id !== example.id);
      entry = entry; // examples are not updated without this
      return;
    }
    if (!await dialogService.promptDelete($t`Example sentence`, '#' + (sense.exampleSentences.indexOf(example) + 1))) return;
    sense.exampleSentences = sense.exampleSentences.filter(e => e.id !== example.id);
    ondelete?.({entry, sense, example});
    entry = entry; // examples are not updated without this
  }
  function moveExample(sense: ISense, example: IExampleSentence, i: number) {
    sense.exampleSentences.splice(sense.exampleSentences.indexOf(example), 1);
    sense.exampleSentences.splice(i, 0, example);
    onExampleChange(sense, example);
    highlighted = { entity: example };
    entry = entry; // examples are not updated without this
  }

  function onSenseChange(sense: ISense) {
    newSenses = newSenses.filter(s => s.id !== sense.id);
    onchange?.({entry, sense});
  }
  function onExampleChange(sense: ISense, example: IExampleSentence) {
    newExamples = newExamples.filter(e => e.id !== example.id);
    onchange?.({entry, sense, example});
  }

  let editorElem: HTMLDivElement | null = $state(null);
  let highlighted = $state<{ entity: IExampleSentence | ISense; autofocus?: boolean}>();
  let highlightTimeout: ReturnType<typeof setTimeout>;
  const ENTITY_FIELD_CONTAINER_CLASS = 'entity-field-container';

  watch(() => highlighted, () => {
    if (highlighted) {
      clearTimeout(highlightTimeout);
      highlightTimeout = setTimeout(() => highlighted = undefined, 3000);
      // wait for rendering
      setTimeout(() => {
        const newEntityElem = editorElem?.querySelector('.highlight');
        if (newEntityElem) {
          if (highlighted?.autofocus && !IsMobile.value)
            findFirstTabbable(newEntityElem?.querySelector(`.${ENTITY_FIELD_CONTAINER_CLASS}`))?.focus();

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
  });

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

  const currentView = useCurrentView();
</script>

<Editor.Root bind:ref>
  <Editor.Grid bind:ref={editorElem}>
    <EntryEditorPrimitive class={ENTITY_FIELD_CONTAINER_CLASS} bind:entry {readonly} {autofocus} {modalMode} onchange={(entry) => onchange?.({entry})} />

    {#each entry.senses as sense, i (sense.id)}
      <Editor.SubGrid class={cn(sense.id === highlighted?.entity.id && 'highlight')}>
        <div id="sense{i + 1}"></div> <!-- shouldn't be in the sticky header -->

        <ObjectHeader type="sense" index={i + 1} class={cn(modalMode || 'sticky',
          'top-0 bg-background z-[1] w-[calc(100%+2px)] pr-[2px] animate-fade-out animation-scroll')}>
          <EntityListItemActions {i}
              items={entry.senses}
              getDisplayName={(sense) => writingSystemService.firstDefOrGlossVal(sense)}
              {readonly}
              onmove={(newIndex) => moveSense(sense, newIndex)}
              ondelete={() => deleteSense(sense)} id={sense.id} />
        </ObjectHeader>

        <SenseEditorPrimitive class={ENTITY_FIELD_CONTAINER_CLASS} bind:sense={entry.senses[i]} {readonly} onchange={() => onSenseChange(sense)}/>

        {#if sense.exampleSentences.length}
          <Editor.SubGrid class="border-l border-dashed pl-4 mt-4 space-y-4 rounded-lg">
            {#each sense.exampleSentences as example, j (example.id)}
              <Editor.SubGrid class={cn(example.id === highlighted?.entity.id && 'highlight')}>
                <div id="example{i + 1}-{j + 1}"></div> <!-- shouldn't be in the sticky header -->
                <ObjectHeader type="example" index={j + 1}>
                  <EntityListItemActions i={j} {readonly}
                                        items={sense.exampleSentences}
                                        getDisplayName={example => writingSystemService.firstSentenceOrTranslationVal(example)}
                                        onmove={(newIndex) => moveExample(sense, example, newIndex)}
                                        ondelete={() => deleteExample(sense, example)}
                                        id={example.id} />
                </ObjectHeader>

                <ExampleEditorPrimitive
                  class={ENTITY_FIELD_CONTAINER_CLASS}
                  bind:example={sense.exampleSentences[j]}
                  {readonly}
                  onchange={() => onExampleChange(sense, example)}
                  />
              </Editor.SubGrid>
            {/each}
          </Editor.SubGrid>
        {/if}
        {#if !readonly && canAddExample}
          <div class="col-span-full flex justify-end mt-4">
            <Button onclick={() => addExample(sense)} icon="i-mdi-plus" size="xs">
              {$t`Add Example`}
            </Button>
          </div>
        {/if}
      </Editor.SubGrid>
    {/each}
    {#if !readonly && canAddSense}
      <hr class="col-span-full grow border-t-4">
      {#if IsMobile.value && !modalMode}
        <FabContainer class="sticky col-span-full mt-2">
          <!-- sticky isn't working in the new entry dialog. I think that's fine/good. -->
          <AddSenseFab onclick={addSense} />
        </FabContainer>
      {:else}
        <div class="col-span-full flex justify-end">
          <AddSenseButton onclick={addSense} />
        </div>
      {/if}
    {/if}
    <DevContent>
      <details>
        <summary>Entry as JSON</summary>
        <code class="col-span-3 whitespace-pre">
          {JSON.stringify(entry, null, '  ')}
        </code>
      </details>
    </DevContent>
  </Editor.Grid>
</Editor.Root>

<style lang="postcss" global>
  .highlight {
    & :is(h2, h3) {
    }

    & hr {
    }
  }
</style>
