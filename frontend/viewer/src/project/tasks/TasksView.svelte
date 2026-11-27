<script lang="ts">
  import {taskLabel, useTasksService, type Task} from './tasks-service';
  import {pt, tvt} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {plural, t} from 'svelte-i18n-lingui';
  import {useDebounce, watch} from 'runed';
  import {useProjectEventBus} from '$lib/services/event-bus';
  import {QueryParamState} from '$lib/utils/url.svelte';
  import {useWritingSystemService} from '$project/data';
  import {type IWritingSystem, WritingSystemType} from '$lib/dotnet-types';
  import {useTasksStats} from './tasks-stats.svelte';
  import TaskView from './TaskView.svelte';
  import TaskList from './TaskList.svelte';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {SidebarTrigger} from '$lib/components/ui/sidebar';
  import ViewErrorBoundary from '$lib/layout/ViewErrorBoundary.svelte';
  import {useProjectStorage} from '$lib/storage';
  import {onErrorEvent} from '$lib/errors/global-errors';

  // The open task lives in the URL so Back and reload work.
  const openTask = new QueryParamState({key: 'taskId', allowBack: true, replaceOnDefaultValue: true});
  const tasksService = useTasksService();
  const viewService = useViewService();
  const writingSystemService = useWritingSystemService();
  const selectedTask = $derived(tasksService.listTasks().find(task => task.id === openTask.current));
  // Subscribed here, not in the cached stats, so counting stops when the view closes.
  // Entering the view recounts anyway, since not every write is announced (the local API's aren't).
  const statsResource = useTasksStats();
  watch([() => tasksService.listTasks().map(task => task.id).join(), () => selectedTask?.id], ([, taskId]) => {
    statsResource.current.openTaskId = taskId;
    void statsResource.refetch();
  });
  $effect(() => {
    if (statsResource.error) onErrorEvent(statsResource.error);
  });
  const debouncedRefetch = useDebounce(() => void statsResource.refetch(), 500);
  useProjectEventBus().onEntriesChanged(() => void debouncedRefetch());

  // Here rather than in the list, so tasks opened from the Dashboard or a link count as recent too.
  const projectStorage = useProjectStorage();
  watch([() => selectedTask?.id, () => projectStorage.recentTasks.loading], ([taskId, loading]) => {
    // Writing before the stored list has loaded would overwrite it.
    if (!taskId || loading) return;
    const known = tasksService.listTasks().map(task => task.id);
    const stored = projectStorage.recentTasks.current.split(',').filter(id => id !== taskId && known.includes(id));
    void projectStorage.recentTasks.set([taskId, ...stored].slice(0, 5).join());
  });

  let lastOrigin: 'recent' | 'list' = 'list';
  function selectTask(taskId: string, origin: 'recent' | 'list') {
    lastOrigin = origin;
    openTask.current = taskId;
  }
  function closeTask() {
    openTask.current = '';
  }
  let lastTask = $state<{id: string, origin: 'recent' | 'list'}>();
  // pre, so the remounting list already has it when its own watch first runs.
  watch.pre(() => openTask.current, (current, previous) => {
    if (!current && previous) lastTask = {id: previous, origin: lastOrigin};
  });

  function writingSystemOf(task: Task): IWritingSystem | undefined {
    if (!task.subjectWritingSystemId) return undefined;
    const writingSystems = task.subjectWritingSystemType === WritingSystemType.Vernacular
      ? writingSystemService.vernacular : writingSystemService.analysis;
    return writingSystems.find(ws => ws.wsId === task.subjectWritingSystemId);
  }
  const selectedWs = $derived(selectedTask && writingSystemOf(selectedTask));

  // Same value the list's "N to go" chip shows; ticks down as entries get filled.
  const stats = $derived(statsResource.current);
  const remaining = $derived(selectedTask ? stats.progress[selectedTask.id]?.remaining : undefined);

  function remainingText(count: number): string {
    return pt(
      $plural(count, {one: '# entry to go', other: '# entries to go'}),
      $plural(count, {one: '# word to go', other: '# words to go'}),
      viewService.currentView);
  }
</script>

<div class="flex flex-col h-full p-4 gap-4">
  <div class="flex flex-row items-center gap-1 min-w-0">
    <SidebarTrigger icon="i-mdi-menu" class="aspect-square shrink-0 p-0" />
    {#if openTask.current}
      <Button variant="ghost" size="icon" class="shrink-0" icon="i-mdi-arrow-left" iconProps={{class: 'rtl:rotate-180'}} onclick={closeTask} aria-label={$t`Back to tasks`} />
    {/if}
    <!-- One line at every width. The header queries its OWN width (not the viewport — the sidebar makes viewport
         a poor proxy) and drops the least-important parts as it narrows, in order: full name + dash, then the
         count. Field name and writing-system abbreviation always stay; the field name ellipsis-truncates last. -->
    <div class="@container flex min-w-0 flex-1 items-baseline gap-x-2">
      <h1 class="min-w-0 truncate text-xl font-semibold">{selectedTask ? pt($tvt(taskLabel(selectedTask)), viewService.currentView) : $t`Tasks`}</h1>
      {#if selectedWs}
        <span class="flex shrink-0 items-baseline gap-x-2 text-xl font-normal">
          <!-- Dash and full name only when the header is wide enough; otherwise the abbreviation is the label. -->
          <span class="text-muted-foreground hidden @md:inline">—</span>
          <!-- items-baseline so the name and abbreviation share a baseline; the audio icon is centered out of it. -->
          <span class="flex items-baseline gap-1.5 {writingSystemService.wsColor(selectedWs.wsId, selectedWs.type === WritingSystemType.Vernacular ? 'vernacular' : 'analysis')}">
            <span class="@md:hidden">{selectedWs.abbreviation || selectedWs.name}</span>
            <span class="hidden @md:inline">{selectedWs.name}</span>
            {#if selectedWs.abbreviation}<span class="text-muted-foreground hidden text-sm @md:inline">{selectedWs.abbreviation}</span>{/if}
            <!-- Audio marker trails the language like a tag; the mic matches the app's record/audio icon. -->
            {#if selectedWs.isAudio}<Icon icon="i-mdi-microphone" class="size-5 self-center" /><span class="sr-only">{$t`Audio`}</span>{/if}
          </span>
        </span>
      {/if}
      {#if selectedTask && remaining}
        <!-- Pinned to the right; dropped on the narrowest headers (field name + abbreviation take priority). Hidden at 0. -->
        <span class="text-muted-foreground hidden shrink-0 text-sm tabular-nums @xs:block @xs:ms-auto">{remainingText(remaining)}</span>
      {/if}
    </div>
  </div>
  <ViewErrorBoundary class="flex-1 min-h-0 overflow-auto" title={$t`Task view failed`}>
    {#if openTask.current}
      <TaskView taskId={openTask.current} onClose={closeTask}/>
    {:else}
      <TaskList onSelect={selectTask} {lastTask} />
    {/if}
  </ViewErrorBoundary>
</div>
