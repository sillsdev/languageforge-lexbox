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
  const writingSystemService = useWritingSystemService();
  const shownFields = $derived([...task.subjectFields, ...task.optionalFields ?? []]);
  const overrides = $derived.by((): Overrides => {
    if (!task.subjectWritingSystemId) return {};
    const ws = {wsId: task.subjectWritingSystemId};
    // An optional field of the other writing system type would otherwise render no inputs
    // at all, since the type the task isn't about is emptied.
    const analysis = writingSystemService.defaultAnalysis;
    const otherType = task.optionalFields?.length && analysis ? [{wsId: analysis.wsId}] : [];
    if (task.subjectWritingSystemType === WritingSystemType.Analysis) {
      return {
        analysis: [ws],
        vernacular: [],
      };
    } else {
      return {
        analysis: otherType,
        vernacular: [ws],
      };
    }
  });
  const entryPersistence = new EntryPersistence(() => entry);
  //need to create a snapshot, otherwise changes to the subjects will trigger this derived and it will skip to the next subject
  let subjects = $derived(TasksService.subjects(task, $state.snapshot(entry)));
  let subjectIndex = $state(0);
  let subject = $derived(subjects.at(subjectIndex));
  // Editing the entry rebuilds the snapshot above, so keying the form on the subject object
  // would tear the editor down on every keystroke. The ids identify the same subject.
  const subjectKey = $derived(subject && [subject.entry.id, subject.sense?.id, subject.exampleSentence?.id].join('|'));
  $effect(() => {
    if (entry && subjects.length === 0) {
      onNextEntry();
    }
    subjectIndex = 0;
  });

  let editor = $state<Editor.Root>();

  async function onNext(skip: boolean = false) {
    if (!skip) {
      if (!subject || !canContinue) return;

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

  // Anything the user typed is worth keeping, and Next is the only thing that saves it, so
  // an optional field alone has to be enough to continue. The editors' change handlers only
  // fire on blur, so this tracks the form's input events instead.
  let edited = $state(false);
  const canContinue = $derived(!!subject && (edited || isSubjectComplete()));

  function subjectEntity() {
    const entity = task.subjectType === 'example-sentence' ? subject?.exampleSentence :
                   task.subjectType === 'entry' ? subject?.entry :
                   subject?.sense;
    if (!entity) throw new Error('Subject entity is undefined');
    return entity;
  }

  function isSubjectComplete() {
    if (!subject) return false;

    return task.isComplete(subjectEntity());
  }

  let form = $state<HTMLFormElement>();
  let focusedSubject: string | undefined;
  // Once per subject: re-focusing on every render would pull the caret out of any other
  // field the user moved to.
  $effect(() => {
    if (!form || !subjectKey || focusedSubject === subjectKey) return;
    focusedSubject = subjectKey;
    edited = false;
    const inputs = form.querySelectorAll<HTMLElement>('input, .ProseMirror');
    for (const input of inputs) {
      if (input.checkVisibility()) {
        input.focus();
        return;
      }
    }
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
        <DictionaryEntry {entry} headwordClass="text-2xl" highlightSenseId={subject?.sense?.id} hideExamples={task.subjectType !== 'example-sentence'}/>
      {/if}
    </div>
    <Drawer.Footer class="gap-4">
      <Separator/>
      <p class="text-lg">
        {task.prompt}
      </p>
      {#if subject}
        {#key subjectKey}
          <form bind:this={form} oninput={() => edited = true} onsubmit={(e) => {e.preventDefault(); void onNext()}}>
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
        <Button onclick={() => onNext()} disabled={!canContinue}>{$t`Next`}</Button>
      </div>
    </Drawer.Footer>
  </Drawer.Content>
</Drawer.Root>
