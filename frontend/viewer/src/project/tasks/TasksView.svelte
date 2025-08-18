<script lang="ts">
  import {SidebarTrigger} from '$lib/components/ui/sidebar';
  import {ResizableHandle, ResizablePane, ResizablePaneGroup} from '$lib/components/ui/resizable';
  import {useTasksService} from './tasks-service';
  import {t} from 'svelte-i18n-lingui';
  import ListItem from '$lib/components/ListItem.svelte';
  import {QueryParamState} from '$lib/utils/url.svelte';
  import TaskView from './TaskView.svelte';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';

  const tasksService = useTasksService();
  const selectedTaskId = new QueryParamState({key: 'taskId', allowBack: true, replaceOnDefaultValue: true});

  const showTaskList = $derived(!selectedTaskId.current || !IsMobile.value);
  const showTaskDetails = $derived(selectedTaskId.current || !IsMobile.value);
</script>

<div class="flex flex-col h-full p-4">
  <ResizablePaneGroup direction="horizontal" class="flex-1">
    {#if showTaskList}
      <ResizablePane defaultSize={20} minSize={10} maxSize={40} class="mr-3">
        <header class="flex items-center gap-2 mb-4">
          <SidebarTrigger icon="i-mdi-menu" iconProps={{ class: 'size-5' }} class="aspect-square p-0" size="xs"/>
          <h1 class="text-xl font-semibold">Tasks</h1>
        </header>
        <div class="flex flex-col h-full p-2">
          {#each tasksService.listTasks() as task (task.id)}
            <ListItem icon="i-mdi-checkbox-outline"
                      class="mb-2"
                      selected={selectedTaskId.current === task.id}
                      onclick={() => selectedTaskId.current = task.id}>
              {task.subject}
            </ListItem>
          {:else}
            <p>{$t`No tasks`}</p>
          {/each}
        </div>
      </ResizablePane>
    {/if}
    {#if showTaskList && showTaskDetails}
      <ResizableHandle class="my-4" withHandle/>
    {/if}
    {#if showTaskDetails}
      <ResizablePane defaultSize={80} minSize={10} maxSize={100} class="ml-3">
        {#if selectedTaskId.current}
          <TaskView taskId={selectedTaskId.current} onClose={() => selectedTaskId.current = undefined}/>
        {:else}
          <p>{$t`Select a task`}</p>
        {/if}
      </ResizablePane>
    {/if}
  </ResizablePaneGroup>


</div>
