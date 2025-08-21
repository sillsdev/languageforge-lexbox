<script lang="ts">
  import {type IEntry, WritingSystemType} from '$lib/dotnet-types';
  import * as Drawer from '$lib/components/ui/drawer';
  import * as Editor from '$lib/components/editor';
  import {Button} from '$lib/components/ui/button';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import {type Task, TasksService, type TaskSubject} from './tasks-service';
  import OverrideFields from '$lib/OverrideFields.svelte';
  import SenseEditorPrimitive from '$lib/entry-editor/object-editors/SenseEditorPrimitive.svelte';
  import EntryEditorPrimitive from '$lib/entry-editor/object-editors/EntryEditorPrimitive.svelte';
  import ExampleEditorPrimitive from '$lib/entry-editor/object-editors/ExampleEditorPrimitive.svelte';
  import {Separator} from '$lib/components/ui/separator';
  import DictionaryEntry from '$lib/DictionaryEntry.svelte';
  import {EntryPersistence} from '$lib/entry-editor/entry-persistence.svelte';

  const writingSystemService = useWritingSystemService();
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
  const overrides = $derived.by(() => {
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
  let subjects = $derived(TasksService.subjects(task, entry));
  let subjectIndex = $state(0);
  let subject = $derived(subjects[subjectIndex]);
  $effect(() => {
    if (entry && subjects.length === 0) {
      onNextEntry();
    }
    subjectIndex = 0;
  });


  async function onNext(skip: boolean = false) {
    if (!skip) {
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
    subjectIndex++;
    if (subjectIndex >= subjects.length) {
      onNextEntry();
    }
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
{@debug subjects}
<Drawer.Root bind:open={() => !!entry, open => {if (!open) entry = undefined;}}>
  <Drawer.Content>
    <Drawer.Header class="relative">
      <div class="absolute -z-10 inset-0 bg-primary rounded my-2 ml-1"
           style="margin-right: {100 - (progress * 100)}%"></div>
<!--      <Drawer.Title class="text-3xl text-center">{entry ? writingSystemService.headword(entry) : ''}</Drawer.Title>-->
      <Drawer.Close>
        {#snippet child({props})}
          <Button {...props} icon="i-mdi-close" variant="ghost" class="absolute right-2"></Button>
        {/snippet}
      </Drawer.Close>
    </Drawer.Header>
    <div class=" mx-2 md:mx-8  shadow-inner rounded-md p-4">
      {#if entry}
        <DictionaryEntry {entry} headwordClass="text-2xl" highlightSenseId={subject?.sense.id} hideExamples={task.subjectType !== 'example-sentence'}/>
      {/if}
    </div>
    <Drawer.Footer>
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
                  {#if task.subjectType === 'entry' && entry}
                    <EntryEditorPrimitive modalMode {entry}/>
                  {:else if task.subjectType === 'sense' && subject.sense}
                    <SenseEditorPrimitive sense={subject.sense}/>
                  {:else if task.subjectType === 'example-sentence' && subject.exampleSentence}
                    <ExampleEditorPrimitive example={subject.exampleSentence}/>
                  {:else}
                    <p>Subject does not have suitable object of type: {task.subjectType}</p>
                  {/if}
                </OverrideFields>
              </Editor.Grid>
            </Editor.Root>
          </form>
        {/key}
      {:else}
        <p>No subject, unable to create a new {task.subjectType}</p>
      {/if}
      <div class="flex flex-row gap-2 justify-end">
        <Drawer.Close>
          {#snippet child({props})}
            <Button {...props} variant="outline">Close</Button>
          {/snippet}
        </Drawer.Close>
        <Button variant="secondary" onclick={() => onNext(true)}>Skip</Button>
        <Button onclick={() => onNext()}>Next</Button>
      </div>
    </Drawer.Footer>
  </Drawer.Content>
</Drawer.Root>
