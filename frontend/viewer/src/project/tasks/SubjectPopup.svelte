<script lang="ts">
  import {type IEntry, WritingSystemType} from '$lib/dotnet-types';
  import * as Drawer from '$lib/components/ui/drawer';
  import * as Editor from '$lib/components/editor';
  import {Button, XButton} from '$lib/components/ui/button';
  import {type Task, TasksService} from './tasks-service';
  import OverrideFields from '$lib/views/OverrideFields.svelte';
  import SenseEditorPrimitive from '$lib/entry-editor/object-editors/SenseEditorPrimitive.svelte';
  import EntryEditorPrimitive from '$lib/entry-editor/object-editors/EntryEditorPrimitive.svelte';
  import ExampleEditorPrimitive from '$lib/entry-editor/object-editors/ExampleEditorPrimitive.svelte';
  import {Separator} from '$lib/components/ui/separator';
  import {EntryPersistence} from '$lib/entry-editor/entry-persistence.svelte';
  import {Progress} from '$lib/components/ui/progress';
  import {t} from 'svelte-i18n-lingui';
  import type {TaskSubject} from './subject.svelte';
  import type {Overrides} from '$lib/views/view-data';
  import DictionaryEntry from '$lib/components/dictionary/DictionaryEntry.svelte';
  import {pt} from '$lib/views/view-text';
  import {untrack} from 'svelte';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {useWritingSystemService} from '$project/data';

  let {
    entry = $bindable(),
    progress = 0,
    task,
    onNextEntry = () => {
    },
    onCompletedSubject = () => {
    }
  }: {
    entry?: IEntry,
    //from 0 - 1
    progress?: number,
    task: Task,
    onNextEntry?: () => void,
    onCompletedSubject?: (subject: TaskSubject) => void,
  } = $props();
  const viewService = useViewService();
  const writingSystemService = useWritingSystemService();
  const shownFields = $derived([...task.subjectFields, ...task.optionalFields ?? []]);
  const overrides = $derived.by((): Overrides => {
    if (!task.subjectWritingSystemId) return {};
    const ws = {wsId: task.subjectWritingSystemId};
    if (task.subjectWritingSystemType === WritingSystemType.Analysis) {
      // the sentence being translated, in every language so audio and out-of-view sentences show too
      if (task.optionalFields?.length) return {analysis: [ws], vernacular: writingSystemService.vernacular.map(({wsId}) => ({wsId}))};
      return {
        analysis: [ws],
        vernacular: [],
      };
    }
    // optional fields (example translations) keep the view's analysis writing systems
    if (task.optionalFields?.length) return {vernacular: [ws]};
    return {
      analysis: [],
      vernacular: [ws],
    };
  });
  const entryPersistence = new EntryPersistence(() => entry);
  //need to create a snapshot, otherwise changes to the subjects will trigger this derived and it will skip to the next subject
  let subjects = $derived(TasksService.subjects(task, $state.snapshot(entry)));
  let subjectIndex = $state(0);
  let subject = $derived(subjects.at(subjectIndex));
  $effect(() => {
    if (entry && subjects.length === 0) {
      onNextEntry();
    }
    subjectIndex = 0;
  });

  let editor = $state<Editor.Root>();

  // Enter or Next again while a save is in flight would complete the same subject twice.
  let saving = false;
  async function onNext(skip: boolean = false) {
    if (saving) return;
    saving = true;
    try {
      await advance(skip);
    } finally {
      saving = false;
    }
  }

  async function advance(skip: boolean) {
    if (!skip) {
      if (!subject || !isSubjectComplete()) return;

      await editor?.commit();

      switch (task.subjectType) {
        case 'example-sentence':
          if (!subject.exampleSentence) throw new Error('Example sentence is undefined');
          await entryPersistence.updateExample(subject.exampleSentence);
          break;
        case 'entry':
          if (!subject.entry) throw new Error('Entry is undefined');
          await entryPersistence.updateEntry(subject.entry);
          break;
        case 'sense':
          if (!subject.sense) throw new Error('Sense is undefined');
          await entryPersistence.updateSense(subject.sense);
      }
      //update subject
      onCompletedSubject(subject);
    }

    if (subjectIndex + 1 >= subjects.length) {
      onNextEntry();
    } else {
      subjectIndex++;
    }
  }

  function isSubjectComplete() {
    if (!subject) return false;

    var subjectEntity = task.subjectType === 'example-sentence' ? subject.exampleSentence :
                        task.subjectType === 'entry' ? subject.entry :
                        task.subjectType === 'sense' ? subject.sense : null;

    if (!subjectEntity) throw new Error('Subject entity is undefined');

    return task.isComplete(subjectEntity);
  }

  let form = $state<HTMLFormElement>();
  $effect(() => {
    if (!form) return;
    let inputs = form.querySelectorAll<HTMLElement>('input, .ProseMirror');
    // focusing reads editor state, which would re-run this and pull the caret back from any other field
    untrack(() => {
      const visible = [...inputs].filter(input => input.checkVisibility());
      // multi-ws inputs are id'd `${rootId}-${wsId}`; the task's empty field beats context fields like the sentence being translated
      const own = visible.filter(input => input.id.endsWith(`-${task.subjectWritingSystemId}`));
      const candidates = own.length ? own : visible;
      const target = candidates.find(input => !(input instanceof HTMLInputElement ? input.value : input.textContent)) ?? candidates[0];
      target?.focus();
    });
  });
</script>

<Drawer.Root bind:open={() => !!entry, open => {if (!open) entry = undefined;}}>
  <Drawer.Content class="mx-auto max-w-4xl">
    <XButton onclick={() => entry = undefined} class="absolute right-2 top-2 z-10" />
    <Drawer.Header class="flex flex-nowrap items-center">
      <Progress value={progress * 100} class="h-8" />
<!--      <Drawer.Title class="text-3xl text-center">{entry ? writingSystemService.headword(entry) : ''}</Drawer.Title>-->
    </Drawer.Header>
    <div class="mx-2 md:mx-4 shadow-inner rounded-md p-4">
      {#if entry}
        <DictionaryEntry {entry} headwordClass="text-2xl" highlightSenseId={subject?.sense?.id} highlightExampleId={subject?.exampleSentence?.id} hideExamples={task.subjectType !== 'example-sentence'}/>
      {/if}
    </div>
    <Drawer.Footer class="gap-4">
      <Separator/>
      <p class="text-lg">
        {pt(task.prompt, viewService.currentView)}
      </p>
      {#if subject}
        {#key subject}
          <form bind:this={form} onsubmit={(e) => {e.preventDefault(); void onNext()}}>
            <!--        lets us submit by pressing enter on any field-->
            <input type="submit" style="display: none;"/>
            <Editor.Root bind:this={editor}>
              <Editor.Grid>
                <OverrideFields {shownFields} {overrides}>
                  {#if task.subjectType === 'entry' && subject.entry}
                    <EntryEditorPrimitive autofocus modalMode bind:entry={subject.entry}/>
                  {:else if task.subjectType === 'sense' && subject.sense}
                    <SenseEditorPrimitive bind:sense={subject.sense}/>
                  {:else if task.subjectType === 'example-sentence' && subject.exampleSentence}
                    <ExampleEditorPrimitive bind:example={subject.exampleSentence}/>
                  {:else}
                    <p>{$t`Subject does not have suitable object of type: ${task.subjectType}`}</p>
                  {/if}
                </OverrideFields>
              </Editor.Grid>
            </Editor.Root>
          </form>
        {/key}
      {:else}
        <p>{$t`No subject, unable to create a new ${task.subjectType}`}</p>
      {/if}
      <div class="flex flex-row gap-2 justify-end">
        <Drawer.Close>
          {#snippet child({props})}
            <Button {...props} variant="secondary">{$t`Close`}</Button>
          {/snippet}
        </Drawer.Close>
        <Button variant="secondary" onclick={() => onNext(true)}>{$t`Skip`}</Button>
        <Button onclick={() => onNext()} disabled={!isSubjectComplete()}>{$t`Next`}</Button>
      </div>
    </Drawer.Footer>
  </Drawer.Content>
</Drawer.Root>
