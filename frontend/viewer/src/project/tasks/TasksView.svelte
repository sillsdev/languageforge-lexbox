<script lang="ts">
  import * as Select from '$lib/components/ui/select';
  import {useTasksService} from './tasks-service';
  import {t} from 'svelte-i18n-lingui';
  import {QueryParamState} from '$lib/utils/url.svelte';
  import TaskView from './TaskView.svelte';

  const tasksService = useTasksService();
  const tasks = $derived(tasksService.listTasks());
  const selectedTaskId = new QueryParamState({key: 'taskId', allowBack: true, replaceOnDefaultValue: true});
  const selectedTask = $derived(tasks.find(task => task.id === selectedTaskId.current));
</script>

<div class="flex flex-col h-full p-4 gap-4">
  <Select.Root type="single" bind:value={selectedTaskId.current}>
    <Select.Trigger>Task {selectedTask?.subject}</Select.Trigger>
    <Select.Content>
      {#each tasks as task (task.id)}
        <Select.Item value={task.id}>{task.subject}</Select.Item>
      {/each}
    </Select.Content>
  </Select.Root>
  {#if selectedTaskId.current}
    <TaskView taskId={selectedTaskId.current} onClose={() => selectedTaskId.current = undefined}/>
  {:else}
    <p>{$t`Select a task`}</p>
  {/if}
</div>
