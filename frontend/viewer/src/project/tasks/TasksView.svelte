<script lang="ts">
  import * as Select from '$lib/components/ui/select';
  import {useTasksService} from './tasks-service';
  import {t} from 'svelte-i18n-lingui';
  import {useProjectStorage} from '$lib/utils/project-storage.svelte';
  import TaskView from './TaskView.svelte';
  import {onMount} from 'svelte';
  import {SidebarTrigger} from '$lib/components/ui/sidebar';

  const projectStorage = useProjectStorage();
  const tasksService = useTasksService();
  const tasks = $derived(tasksService.listTasks());
  const selectedTask = $derived(tasks.find(task => task.id === projectStorage.selectedTaskId.current));

  onMount(() => {
    if (!projectStorage.selectedTaskId.current) {
      open = true;
    }
  });
  let open = $state(false);
</script>

<div class="flex flex-col h-full p-4 gap-4">
  <div class="flex flex-row items-center">
    <SidebarTrigger icon="i-mdi-menu" class="aspect-square p-0 mr-2" />

    <Select.Root bind:open type="single" bind:value={projectStorage.selectedTaskId.current}>
      <Select.Trigger>{$t`Task ${selectedTask?.subject ?? ''}`}</Select.Trigger>
      <Select.Content>
        {#each tasks as task (task.id)}
          <Select.Item value={task.id}>{task.subject}</Select.Item>
        {/each}
      </Select.Content>
    </Select.Root>
  </div>
  {#if projectStorage.selectedTaskId.current}
    <TaskView taskId={projectStorage.selectedTaskId.current} onClose={() => projectStorage.selectedTaskId.current = ''}/>
  {:else}
    <h1 class="text-xl p-4 mx-auto">{$t`Select a new task to work on`}</h1>
  {/if}
</div>
