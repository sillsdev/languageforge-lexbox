<script lang="ts" module>
  import type {IEntry, IExampleSentence, ISense} from '$lib/dotnet-types';

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
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {initImageService} from '../field-editors/image-service.svelte';
  import {useViewService, hasVisibleFields} from '$lib/views/view-service.svelte';
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
  import DevContent from '$lib/layout/DevContent.svelte';
  import ObjectHeader from './ObjectHeader.svelte';
  import AddSenseButton from './AddSenseButton.svelte';
  import AddObjectDialog from './AddObjectDialog.svelte';
  import OverrideFields from '$lib/views/OverrideFields.svelte';

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

  // Entry-scoped image cache: pictures (and the edit/viewer dialogs) share one load per mediaUri.
  initImageService();

  let editor = $state<Editor.Root>();

  export function commit() {
    return editor?.commit();
  }

  // New senses and examples are drafted in a dialog and only join the entry when the user confirms
  let draftSense = $state<ISense>();
  let draftExample = $state<IExampleSentence>();

  function addSense() {
    draftSense = defaultSense(entry.id);
  }

  function createSense() {
    if (!draftSense) return;
    const sense = $state.snapshot(draftSense);
    entry.senses = [...entry.senses, sense];
    highlighted = { entity: sense };
    onchange?.({entry, sense});
  }

  function addExample(sense: ISense) {
    draftExample = defaultExampleSentence(sense.id);
  }

  function createExample() {
    if (!draftExample) return;
    const example = $state.snapshot(draftExample);
    const sense = entry.senses.find(s => s.id === example.senseId);
    if (!sense) throw new Error(`Sense ${example.senseId} not found`);
    sense.exampleSentences = [...sense.exampleSentences, example];
    entry = entry; // examples counts are not updated without this
    highlighted = { entity: example };
    onchange?.({entry, sense, example});
  }

  const draftExampleSenseIndex = $derived(draftExample ? entry.senses.findIndex(s => s.id === draftExample?.senseId) : -1);
  const draftExampleDescription = $derived.by(() => {
    if (draftExampleSenseIndex < 0) return undefined;
    const sense = entry.senses[draftExampleSenseIndex];
    const label = `${pt($t`Sense`, $t`Meaning`, viewService.currentView)} ${draftExampleSenseIndex + 1}`;
    const gloss = writingSystemService.firstDefOrGlossVal(sense);
    return gloss ? `${label}: ${gloss}` : label;
  });

  async function deleteSense(sense: ISense) {
    if (!await dialogService.promptDelete(pt($t`Sense`, $t`Meaning`, viewService.currentView), writingSystemService.firstGloss(sense))) return;
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
    onchange?.({entry, sense});
  }
  function onExampleChange(sense: ISense, example: IExampleSentence) {
    onchange?.({entry, sense, example});
  }

  let editorElem: HTMLDivElement | null = $state(null);
  let highlighted = $state<{ entity: IExampleSentence | ISense }>();
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

  const viewService = useViewService();
  const showExamples = $derived(hasVisibleFields(viewService.currentView.exampleFields));
  const showSenses = $derived(showExamples || hasVisibleFields(viewService.currentView.senseFields));
</script>

<div class="flex min-h-0 gap-4">
  <Editor.Root bind:ref bind:this={editor} class="min-w-0 flex-1">
    <Editor.Grid bind:ref={editorElem}>
      <EntryEditorPrimitive class={ENTITY_FIELD_CONTAINER_CLASS} bind:entry {readonly} {autofocus} {modalMode} onchange={(entry) => onchange?.({entry})} />

    {#if showSenses}
      {#each entry.senses as sense, i (sense.id)}
        <Editor.SubGrid class={cn(sense.id === highlighted?.entity.id && 'highlight')}>
          <div id="sense{i + 1}"></div> <!-- shouldn't be in the sticky header -->

          <ObjectHeader type="sense" index={i + 1} class={cn(modalMode || 'sticky',
            'top-0 bg-background z-1 w-[calc(100%+2px)] pr-0.5 animate-fade-out animation-scroll')}>
            <EntityListItemActions {i}
                items={entry.senses}
                getDisplayName={(sense) => writingSystemService.firstDefOrGlossVal(sense)}
                {readonly}
                onmove={(newIndex) => moveSense(sense, newIndex)}
                ondelete={() => deleteSense(sense)} id={sense.id} />
          </ObjectHeader>

          <SenseEditorPrimitive class={ENTITY_FIELD_CONTAINER_CLASS} bind:sense={entry.senses[i]} {readonly} onchange={() => onSenseChange(sense)}/>

          {#if showExamples && sense.exampleSentences.length}
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
                                           id={example.id}/>
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
          {#if showExamples && !readonly && canAddExample}
            <div class="col-span-full flex justify-end mt-4">
              <Button onclick={() => addExample(sense)} icon="i-mdi-plus" size="xs">
                {$t`Add Example`}
              </Button>
            </div>
          {/if}
        </Editor.SubGrid>
      {/each}
    {/if}
    {#if showSenses && !readonly && canAddSense}
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
</div>

<AddObjectDialog
  bind:open={() => !!draftSense, (open) => { if (!open) draftSense = undefined; }}
  title={pt($t`Add Sense`, $t`Add Meaning`, viewService.currentView)}
  submitLabel={pt($t`Add Sense`, $t`Add Meaning`, viewService.currentView)}
  onsubmit={createSense}>
  {#if draftSense}
    <!-- Pictures are saved straight to the API against an existing sense, so they can only be added once it exists -->
    <OverrideFields overrides={{senseFields: viewService.currentView.senseFields.filter(f => f.fieldId !== 'pictures')}}>
      <SenseEditorPrimitive bind:sense={draftSense} />
    </OverrideFields>
  {/if}
</AddObjectDialog>

<AddObjectDialog
  bind:open={() => !!draftExample, (open) => { if (!open) draftExample = undefined; }}
  title={$t`Add Example`}
  description={draftExampleDescription}
  submitLabel={$t`Add Example`}
  onsubmit={createExample}>
  {#if draftExample}
    <ExampleEditorPrimitive bind:example={draftExample} />
  {/if}
</AddObjectDialog>

<style lang="postcss">
  @reference "#app.css";

  :global(.highlight) {
    :global(h2, h3) {
      @apply text-primary;
    }

    :global(hr) {
      @apply border-primary;
    }
  }
</style>
