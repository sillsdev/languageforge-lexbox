<script lang="ts">
  import {type IEntry, WritingSystemType} from '$lib/dotnet-types';
  import * as Drawer from '$lib/components/ui/drawer';
  import * as Editor from '$lib/components/editor';
  import {Button} from '$lib/components/ui/button';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import {type Task, TasksService} from './tasks-service';
  import OverrideFields from '$lib/OverrideFields.svelte';
  import SenseEditorPrimitive from '$lib/entry-editor/object-editors/SenseEditorPrimitive.svelte';
  import EntryEditorPrimitive from '$lib/entry-editor/object-editors/EntryEditorPrimitive.svelte';
  import ExampleEditorPrimitive from '$lib/entry-editor/object-editors/ExampleEditorPrimitive.svelte';
  import {Separator} from '$lib/components/ui/separator';
  import {defaultExampleSentence, defaultSense} from '$lib/utils';
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
    onCompletedSubject?: (subject: string) => void,
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
  let senseIndex = $derived(entry ? TasksService.findNextSubjectIndex(task, entry, 0) : 0);
  let sense = $derived.by(() => {
    if (!entry) return undefined;
    return $state.snapshot(entry.senses[senseIndex])
      //don't create a sense when the subject is an example sentence
      ?? (task.subjectType === 'sense' ? defaultSense(entry.id) : undefined);
  });
  let exampleIndex = $derived(sense ? TasksService.findNextSubjectIndex(task, sense, 0) : 0);
  let example = $derived.by(() => {
    if (!sense || task.subjectType !== 'example-sentence') return undefined;
    return $state.snapshot(sense.exampleSentences[exampleIndex])
      ?? (task.subjectType === 'example-sentence' ? defaultExampleSentence(sense.id) : undefined);
  });

  async function onNext(skip: boolean = false) {
    if (!entry) return;
    //comparing directly to a bool value to avoid comparing against an event
    let updateExample = skip === false;
    let updateSense = skip === false;
    let updateEntry = skip === false;
    switch (task.subjectType) {
      case 'example-sentence':
        if (updateExample) {
          await entryPersistence.updateExample(example);
          const subject = task.getSubjectValue(example);
          onCompletedSubject(subject);
        }
        exampleIndex = TasksService.findNextSubjectIndex(task, sense, exampleIndex + 1);
        if (sense && exampleIndex < sense.exampleSentences.length) {
          return;
        }
        updateSense = false;
      // eslint-disable-next-line no-fallthrough
      case 'sense':
        if (updateSense) {
          await entryPersistence.updateSense(sense);
          entry = entryPersistence.initialEntry;
          const subject = task.getSubjectValue(sense);
          onCompletedSubject(subject);
        }
        senseIndex = TasksService.findNextSubjectIndex(task, entry, senseIndex + 1);
        exampleIndex = 0;
        if (entry && senseIndex < entry.senses.length) {
          return;
        }

        updateEntry = false;
      // eslint-disable-next-line no-fallthrough
      case 'entry':
        if (updateEntry) {
          await entryPersistence.updateEntry(entry);
          const subject = task.getSubjectValue(entry);
          onCompletedSubject(subject);
        }
        senseIndex = 0;
        onNextEntry();
        break;
    }
  }

  let form = $state<HTMLFormElement>();
  $effect(() => {
    if (!form) return;
    let inputs = form?.getElementsByTagName('input');

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
  <Drawer.Content>
    <Drawer.Header class="relative">
      <div class="absolute -z-10 inset-0 bg-primary rounded my-2 ml-1"
           style="margin-right: {100 - (progress * 100)}%"></div>
      <Drawer.Title>{entry ? writingSystemService.headword(entry) : ''}</Drawer.Title>
      <Drawer.Close>
        {#snippet child({props})}
          <Button {...props} icon="i-mdi-close" variant="ghost" class="absolute right-2"></Button>
        {/snippet}
      </Drawer.Close>
    </Drawer.Header>
    <div class="flex flex-col gap-4 mx-2 md:mx-4 border rounded p-4 max-h-[50vh] overflow-y-auto">
      <Editor.Root>
        <Editor.Grid>
          <OverrideFields shownFields={task.contextFields}>
            {#if entry}
              <EntryEditorPrimitive readonly {entry}/>
            {/if}
            {#if sense}
              <SenseEditorPrimitive readonly {sense}/>
            {:else if task.subjectType === 'example-sentence'}
              <p>No sense, unable to create a new example sentence</p>
            {/if}
            {#if example}
              <ExampleEditorPrimitive readonly {example}/>
            {/if}
          </OverrideFields>
        </Editor.Grid>
      </Editor.Root>
    </div>
    <Drawer.Footer>
      <Separator/>
      <p class="text-sm">
        {task.prompt}
      </p>
      <form bind:this={form} onsubmit={(e) => {e.preventDefault(); void onNext()}}>
        <!--        lets us submit by pressing enter on any field-->
        <input type="submit" style="display: none;"/>
        <Editor.Root>
          <Editor.Grid>
            <OverrideFields shownFields={task.subjectFields} {overrides}>
              {#if task.subjectType === 'entry' && entry}
                <EntryEditorPrimitive modalMode {entry}/>
              {:else if task.subjectType === 'sense' && sense}
                <SenseEditorPrimitive {sense}/>
              {:else if task.subjectType === 'example-sentence' && example}
                <ExampleEditorPrimitive {example}/>
              {:else}
                <p>No subject, unable to create a new {task.subjectType}</p>
              {/if}
            </OverrideFields>
          </Editor.Grid>
        </Editor.Root>
      </form>
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
