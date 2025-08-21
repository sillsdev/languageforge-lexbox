<script lang="ts">
  import {type TaskSubject, useTasksService} from './tasks-service';
  import {type IEntry, SortField} from '$lib/dotnet-types';
  import EntriesList from '../browse/EntriesList.svelte';
  import {Button} from '$lib/components/ui/button';
  import SubjectPopup from './SubjectPopup.svelte';
  import DevContent from '$lib/layout/DevContent.svelte';
  import DoneView from './DoneView.svelte';
  import {watch} from 'runed';

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
  let completedSubjects = $state([] as TaskSubject[]);
  let progress = $derived(completedSubjects.length / 10);
  let showDone = $state(false);
  let allCompletedSubjects = $state([] as TaskSubject[]);

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
      <EntriesList bind:this={entriesList}
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
          if (completedSubjects.length === 10) {
            onDone();
          }
        }}
      />
    {:else}
      <DoneView subjects={completedSubjects}
                allSubjects={allCompletedSubjects}
                {task}
                onFinish={() =>onClose()}
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
