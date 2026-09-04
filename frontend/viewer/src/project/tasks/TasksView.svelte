<script lang="ts">
  import {useTasksService} from './tasks-service';
  import {t} from 'svelte-i18n-lingui';
  import {useProjectStorage} from '$lib/storage';
  import TaskView from './TaskView.svelte';
  import TaskList from './TaskList.svelte';
  import {Button} from '$lib/components/ui/button';
  import {SidebarTrigger} from '$lib/components/ui/sidebar';
  import ViewErrorBoundary from '$lib/layout/ViewErrorBoundary.svelte';

  const selectedTaskId = useProjectStorage().selectedTaskId;
  const tasksService = useTasksService();
  const selectedTask = $derived(tasksService.listTasks().find(task => task.id === selectedTaskId.current));

  let lastTaskId = $state('');
  function closeTask() {
    lastTaskId = selectedTaskId.current;
    void selectedTaskId.set('');
  }
</script>

<div class="flex flex-col h-full p-4 gap-4">
  <div class="flex flex-row items-center gap-2">
    <SidebarTrigger icon="i-mdi-menu" class="aspect-square p-0" />
    {#if selectedTaskId.current}
      <Button variant="ghost" size="icon" icon="i-mdi-arrow-left" onclick={closeTask} aria-label={$t`Back to tasks`} />
    {/if}
    <h1 class="text-xl font-semibold truncate min-w-0">{selectedTask?.fieldLabel ?? $t`Tasks`}</h1>
  </div>
  <ViewErrorBoundary class="flex-1 min-h-0 overflow-auto" title={$t`Task view failed`}>
    {#if selectedTaskId.current}
      <TaskView taskId={selectedTaskId.current} onClose={closeTask}/>
    {:else if !selectedTaskId.loading}
      <TaskList {lastTaskId} onSelect={taskId => selectedTaskId.set(taskId)} />
    {/if}
  </ViewErrorBoundary>
</div>
