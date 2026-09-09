<script lang="ts">
  import {taskLabel, useTasksService, type Task} from './tasks-service';
  import {pt, tvt} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {plural, t} from 'svelte-i18n-lingui';
  import {useProjectStorage} from '$lib/storage';
  import {QueryParamState} from '$lib/utils/url.svelte';
  import {useWritingSystemService} from '$project/data';
  import {type IWritingSystem, WritingSystemType} from '$lib/dotnet-types';
  import {useTasksStats} from './tasks-stats.svelte';
  import TaskView from './TaskView.svelte';
  import TaskList from './TaskList.svelte';
  import {Button} from '$lib/components/ui/button';
  import {SidebarTrigger} from '$lib/components/ui/sidebar';
  import ViewErrorBoundary from '$lib/layout/ViewErrorBoundary.svelte';

  // The open task lives in the URL so Back and reload work; the persisted value only remembers
  // which task to reopen when the view is next entered (e.g. via the sidebar).
  const openTask = new QueryParamState({key: 'taskId', allowBack: true, replaceOnDefaultValue: true});
  const rememberedTask = useProjectStorage().selectedTaskId;
  const tasksService = useTasksService();
  const viewService = useViewService();
  const writingSystemService = useWritingSystemService();
  const stats = $derived(useTasksStats().current);

  const selectedTask = $derived(tasksService.listTasks().find(task => task.id === openTask.current));

  // Reopen the last task once, pushing it onto history so Back returns to the list.
  let reopened = false;
  $effect(() => {
    if (reopened || rememberedTask.loading) return;
    reopened = true;
    if (!openTask.current && rememberedTask.current) openTask.current = rememberedTask.current;
  });

  function selectTask(taskId: string) {
    openTask.current = taskId;
    void rememberedTask.set(taskId);
  }
  function closeTask() {
    openTask.current = '';
  }

  function writingSystemOf(task: Task): IWritingSystem | undefined {
    if (!task.subjectWritingSystemId) return undefined;
    const writingSystems = task.subjectWritingSystemType === WritingSystemType.Vernacular
      ? writingSystemService.vernacular : writingSystemService.analysis;
    return writingSystems.find(ws => ws.wsId === task.subjectWritingSystemId);
  }
  const selectedWs = $derived(selectedTask && writingSystemOf(selectedTask));

  const subtitle = $derived.by(() => {
    if (!selectedTask) return pt($t`Fill in what's missing, one entry at a time.`, $t`Fill in what's missing, one word at a time.`, viewService.currentView);
    const progress = stats.progress[selectedTask.id];
    const remaining = progress && pt(
      $plural(progress.remaining, {one: '# entry to go', other: '# entries to go'}),
      $plural(progress.remaining, {one: '# word to go', other: '# words to go'}),
      viewService.currentView);
    const description = selectedTask.description && pt($tvt(selectedTask.description), viewService.currentView);
    return [remaining, description].filter(Boolean).join(' · ');
  });
</script>

<div class="flex flex-col h-full p-4 gap-4">
  <div class="flex flex-row items-start gap-2">
    <div class="flex h-10 shrink-0 items-center gap-1">
      <SidebarTrigger icon="i-mdi-menu" class="aspect-square p-0" />
      {#if openTask.current}
        <Button variant="ghost" size="icon" icon="i-mdi-arrow-left" onclick={closeTask} aria-label={$t`Back to tasks`} />
      {/if}
    </div>
    <div class="min-w-0">
      <h1 class="text-xl font-semibold leading-10 flex flex-wrap items-baseline gap-x-2">
        <span class="truncate">{selectedTask ? pt($tvt(taskLabel(selectedTask)), viewService.currentView) : $t`Tasks`}</span>
        {#if selectedWs}
          <span class="font-normal flex items-baseline gap-1.5 {writingSystemService.wsColor(selectedWs.wsId, selectedWs.type === WritingSystemType.Vernacular ? 'vernacular' : 'analysis')}">
            {selectedWs.name}
            {#if selectedWs.abbreviation}<span class="text-muted-foreground text-xs">{selectedWs.abbreviation}</span>{/if}
          </span>
        {/if}
      </h1>
      <p class="text-muted-foreground text-sm truncate">{subtitle}</p>
    </div>
  </div>
  <ViewErrorBoundary class="flex-1 min-h-0 overflow-auto" title={$t`Task view failed`}>
    {#if openTask.current}
      <TaskView taskId={openTask.current} onClose={closeTask}/>
    {:else if !rememberedTask.loading}
      <TaskList lastTaskId={rememberedTask.current} onSelect={selectTask} />
    {/if}
  </ViewErrorBoundary>
</div>
