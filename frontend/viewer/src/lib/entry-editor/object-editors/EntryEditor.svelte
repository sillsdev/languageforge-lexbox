<script lang="ts">
  import type {IEntry, IExampleSentence, ISense} from '$lib/dotnet-types';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {useCurrentView} from '$lib/views/view-service';
  import {cn, defaultExampleSentence, defaultSense} from '$lib/utils';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
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

  type Props = {
    entry: IEntry;
    readonly?: boolean;
    modalMode?: boolean;
    canAddSense?: boolean;
    canAddExample?: boolean;
    onchange?: (changed: { entry: IEntry, sense?: ISense, example?: IExampleSentence}) => void;
    ondelete?: (deleted: { entry: IEntry, sense?: ISense, example?: IExampleSentence}) => void;
  };

  let {
    entry = $bindable(),
    readonly = false,
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

  async function deleteSense(sense: ISense) {
    if (newSenses.some(s => s.id === sense.id)) {
      newSenses = newSenses.filter(s => s.id !== sense.id);
      entry.senses = entry.senses.filter(s => s.id !== sense.id);
      return;
    }
    if (!await dialogService.promptDelete('Sense')) return;
    entry.senses = entry.senses.filter(s => s.id !== sense.id);
    ondelete?.({entry, sense});
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
    ondelete?.({entry, sense, example});
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
    onchange?.({entry, sense});
  }
  function onExampleChange(sense: ISense, example: IExampleSentence) {
    newExamples = newExamples.filter(e => e.id !== example.id);
    onchange?.({entry, sense, example});
  }

  let editorElem: HTMLDivElement | null = $state(null);
  let highlightedEntity = $state<IExampleSentence | ISense | undefined>();
  let highlightTimeout: ReturnType<typeof setTimeout>;

  watch(() => highlightedEntity, () => {
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

<Editor.Root>
  <Editor.Grid bind:ref={editorElem}>
    <EntryEditorPrimitive bind:entry {readonly} {modalMode} onchange={(entry) => onchange?.({entry})} />

    {#each entry.senses as sense, i (sense.id)}
      <Editor.SubGrid class={cn(sense.id === highlightedEntity?.id && 'highlight')}>
        <div id="sense{i + 1}"></div> <!-- shouldn't be in the sticky header -->
        <div class="col-span-full flex items-center py-2 mb-1 sticky top-0 bg-background z-[1] w-[calc(100%+2px)] pr-[2px] animate-fade-out animation-scroll">
          <h2 class="text-lg text-muted-foreground">{pt($t`Sense`, $t`Meaning`, $currentView)} {i + 1}</h2>
          <hr class="grow border-t-2 mx-4">
          <EntityListItemActions {i}
              items={entry.senses}
              getDisplayName={(sense) => writingSystemService.firstDefOrGlossVal(sense)}
              {readonly}
              onmove={(newIndex) => moveSense(sense, newIndex)}
              ondelete={() => deleteSense(sense)} id={sense.id} />
        </div>

        <SenseEditorPrimitive bind:sense={entry.senses[i]} {readonly} onchange={() => onSenseChange(sense)}/>

        {#if sense.exampleSentences.length}
          <Editor.SubGrid class="border-l border-dashed pl-4 mt-4 space-y-4 rounded-lg">
            {#each sense.exampleSentences as example, j (example.id)}
              <Editor.SubGrid class={cn(example.id === highlightedEntity?.id && 'highlight')}>
                <div id="example{i + 1}-{j + 1}"></div> <!-- shouldn't be in the sticky header -->
                <div class="col-span-full flex items-center mb-2">
                  <h3 class="text-muted-foreground">{$t`Example`} {j + 1}</h3>
                  <!--
                    <hr class="grow">
                    collapse/expand toggle
                  -->
                  <hr class="grow mx-4">
                  <EntityListItemActions i={j} {readonly}
                                        items={sense.exampleSentences}
                                        getDisplayName={example => writingSystemService.firstSentenceOrTranslationVal(example)}
                                        onmove={(newIndex) => moveExample(sense, example, newIndex)}
                                        ondelete={() => deleteExample(sense, example)}
                                        id={example.id}
                  />
                </div>

                <ExampleEditorPrimitive
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
      <hr class="col-span-full grow border-t-4 my-4">
      <div class="lg-view:hidden flex col-span-full justify-end sticky bottom-3 right-3 z-[2]" class:hidden={modalMode}>
        <!-- sticky isn't working in the new entry dialog. I think that's fine/good. -->
        <AddSenseFab onclick={addSense} />
      </div>
      <div class="col-span-full flex justify-end" class:sm-view:hidden={!modalMode}>
        <Button onclick={addSense} icon="i-mdi-plus" size="xs" title={pt($t`Add Sense`, $t`Add Meaning`, $currentView)}>{pt($t`Add Sense`, $t`Add Meaning`, $currentView)}</Button>
      </div>
    {/if}
  </Editor.Grid>
</Editor.Root>

<style lang="postcss" global>
  .highlight {
    & :is(h2, h3) {
      @apply text-info-500;
    }

    & hr {
      @apply border-info-500;
    }
  }
</style>
