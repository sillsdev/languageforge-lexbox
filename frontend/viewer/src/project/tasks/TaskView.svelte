<script lang="ts">
  import {useTasksService} from './tasks-service';
  import {type IEntry, SortField} from '$lib/dotnet-types';
  import EntriesList from '../browse/EntriesList.svelte';
  import {Button} from '$lib/components/ui/button';
  import SubjectPopup from './SubjectPopup.svelte';
  import DevContent from '$lib/layout/DevContent.svelte';
  import DoneView from './DoneView.svelte';
  import {watch} from 'runed';
  import type {TaskSubject} from './subject.svelte';
  import {t} from 'svelte-i18n-lingui';

  const TASK_SUBJECT_COUNT = 10;

  let {
    taskId,
    onClose = () => {
    }
  }: { taskId: string, onClose: () => void } = $props();
  const tasksService = useTasksService();
  const task = $derived(tasksService.listTasks().find(task => task.id === taskId));
  watch(() => taskId, () => {
    completedSubjects = [];
    showDone = false;
  });
  let entry = $state<IEntry>();

  let entriesList = $state<EntriesList>();
  let entryCount = $state<number | null>(null);
  let completedSubjects = $state<TaskSubject[]>([]);
  let progress = $derived((completedSubjects.length + 1) / Math.min(entryCount ?? TASK_SUBJECT_COUNT, TASK_SUBJECT_COUNT));
  let showDone = $state(false);
  let allCompletedSubjects = $state<TaskSubject[]>([]);

  function onDone() {
    showDone = true;
  }
  function onContinue() {
    showDone = false;
    completedSubjects = [];
  }
</script>
<div class="flex flex-col h-full gap-4">
  {#if task}
    {#if !showDone}
      <DevContent>
        <details>
          <summary>Completed Subjects</summary>
          {completedSubjects.map(s => s.subject).join(', ')}
        </details>
      </DevContent>
      {#if entryCount === 0}
        <h1 class="text-xl p-4 mx-auto">{$t`This task is complete`} 🎊</h1>
      {/if}
      <EntriesList bind:this={entriesList}
                   bind:entryCount
                   sort={{field: SortField.Headword, dir: 'asc'}}
                   gridifyFilter={task?.gridifyFilter}
                   disableNewEntry
                   onSelectEntry={e => entry = e} selectedEntryId={entry?.id}/>
      <Button onclick={onDone}>Done</Button>
      <SubjectPopup
        bind:entry
        {task}
        {progress}
        onNextEntry={() => {
          let next = entriesList?.selectNextEntry();
          if (!next) onDone();
        }}
        onCompletedSubject={subject => {
          completedSubjects.push(subject);
          allCompletedSubjects.push(subject);
          if (completedSubjects.length === TASK_SUBJECT_COUNT) {
            onDone();
          }
        }}
      />
    {:else}
      <DoneView subjects={completedSubjects}
                allSubjects={allCompletedSubjects}
                {task}
                onFinish={() => onClose()}
                onContinue={onContinue}
      />
    {/if}
  {:else}
    <div class="flex flex-col gap-4">
      <div class="flex flex-col gap-4">
        <h1 class="text-2xl font-bold">Task not found: {taskId}</h1>
        <p>The task you are looking for does not exist.</p>
      </div>
    </div>
  {/if}
</div>
