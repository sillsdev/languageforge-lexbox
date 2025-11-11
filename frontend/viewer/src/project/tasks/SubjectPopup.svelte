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
  import {tick} from 'svelte';
  import DictionaryEntry from '$lib/components/dictionary/DictionaryEntry.svelte';

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
  const overrides = $derived.by((): Overrides => {
    if (!task.subjectWritingSystemId) return {};
    if (task.subjectWritingSystemType === WritingSystemType.Analysis) {
      return {
        analysisWritingSystems: [task.subjectWritingSystemId],
        vernacularWritingSystems: [],
      };
    } else {
      return {
        analysisWritingSystems: [],
        vernacularWritingSystems: [task.subjectWritingSystemId]
      };
    }
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


  async function onNext(skip: boolean = false) {
    if (!skip) {
      if (!subject || !isSubjectComplete()) return;

      if (document.activeElement instanceof HTMLElement) {
        // We have change rich-text change handlers that
        // (1) add WS's to spans (i.e. finalize rich-strings. It's maybe a bad idea that we currently do that lazily) and
        // (2) run into errors if triggered too late (e.g. via onDestroy)
        // this is a simple way to ensure they run cleanly before we move on
        document.activeElement.blur();
        await tick();
      }

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
    let inputs = form?.querySelectorAll<HTMLElement>('input, .ProseMirror');

    if (!inputs || inputs.length < 0) return;
    for (let input of inputs) {
      if (input.checkVisibility()) {
        input.focus();
        return;
      }
    }
  });
</script>

<Drawer.Root bind:open={() => !!entry, open => {if (!open) entry = undefined;}}>
  <Drawer.Content class="mx-auto max-w-4xl">
    <Drawer.Header class="relative flex flex-nowrap items-center">
      <Progress value={progress * 100} class="h-8" />
<!--      <Drawer.Title class="text-3xl text-center">{entry ? writingSystemService.headword(entry) : ''}</Drawer.Title>-->
      <XButton onclick={() => entry = undefined} />
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
        {#key subject}
          <form bind:this={form} onsubmit={(e) => {e.preventDefault(); void onNext()}}>
            <!--        lets us submit by pressing enter on any field-->
            <input type="submit" style="display: none;"/>
            <Editor.Root>
              <Editor.Grid>
                <OverrideFields shownFields={task.subjectFields} {overrides}>
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
