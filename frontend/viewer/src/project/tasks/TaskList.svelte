<script lang="ts">
  import ListItem from '$lib/components/ListItem.svelte';
  import * as Collapsible from '$lib/components/ui/collapsible';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {Progress} from '$lib/components/ui/progress';
  import {Skeleton} from '$lib/components/ui/skeleton';
  import {formatNumber} from '$lib/components/ui/format';
  import {useProjectStorage} from '$lib/storage';
  import {useFeatures} from '$lib/services/feature-service';
  import {useWritingSystemService} from '$project/data';
  import {type IWritingSystem, WritingSystemType} from '$lib/dotnet-types';
  import {t} from 'svelte-i18n-lingui';
  import {watch} from 'runed';
  import {useTasksService, type Task} from './tasks-service';
  import {useTasksStats} from './tasks-stats.svelte';

  let {onSelect}: {onSelect: (taskId: string) => void} = $props();

  const storage = useProjectStorage();
  const tasksService = useTasksService();
  const writingSystemService = useWritingSystemService();
  const features = useFeatures();

  // The editors hide audio writing systems when the feature is off, so their tasks would
  // open with nothing to fill in.
  function selectable(writingSystems: IWritingSystem[]): IWritingSystem[] {
    return features.audio ? writingSystems : writingSystems.filter(ws => !ws.isAudio);
  }
  const vernacular = $derived(selectable(writingSystemService.vernacular));
  const analysis = $derived(selectable(writingSystemService.analysis));

  function scoped(writingSystems: IWritingSystem[], stored: string): IWritingSystem | undefined {
    return writingSystems.find(ws => ws.wsId === stored)
      ?? writingSystems.find(ws => !ws.isAudio)
      ?? writingSystems[0];
  }
  const vernacularScope = $derived(scoped(vernacular, storage.taskVernacularWsId.current));
  const analysisScope = $derived(scoped(analysis, storage.taskAnalysisWsId.current));

  function writingSystemOf(task: Task): IWritingSystem | undefined {
    if (!task.subjectWritingSystemId) return undefined;
    return task.subjectWritingSystemType === WritingSystemType.Vernacular ? vernacularScope : analysisScope;
  }

  // One row per field: the writing system comes from the scope above the list, not the row.
  const rows = $derived(tasksService.listTasks()
    .filter(task => !task.subjectWritingSystemId || task.subjectWritingSystemId === writingSystemOf(task)?.wsId)
    .map(task => ({task, ws: writingSystemOf(task)})));

  // Hooks must run while the component initialises, so the resource takes a getter and
  // reads the rows later, on load.
  const statsResource = useTasksStats(() => rows.map(row => row.task));
  const stats = $derived(statsResource.current);
  // Switching scope swaps in tasks the counts were never loaded for.
  watch(() => rows.map(row => row.task.id).join(), () => void statsResource.refetch());
  const todo = $derived(rows.filter(row => stats.progress[row.task.id]?.remaining !== 0));
  const done = $derived(rows.filter(row => stats.progress[row.task.id]?.remaining === 0));

  function wsColor(ws: IWritingSystem): string {
    return writingSystemService.wsColor(ws.wsId, ws.type === WritingSystemType.Vernacular ? 'vernacular' : 'analysis');
  }
</script>

{#snippet wsMark(ws: IWritingSystem)}
  {#if ws.isAudio}
    <Icon icon="i-mdi-microphone" class="size-4 {wsColor(ws)}" />
    <span class="sr-only">{$t`Audio`}</span>
  {:else}
    <span class="{wsColor(ws)} size-2 shrink-0 rounded-full bg-current" aria-hidden="true"></span>
  {/if}
{/snippet}

{#snippet scopePicker(label: string, writingSystems: IWritingSystem[], selected: IWritingSystem, onPick: (wsId: string) => void)}
  <div class="flex flex-col gap-1">
    <span class="text-xs text-muted-foreground">{label}</span>
    <ResponsiveMenu.Root>
      <ResponsiveMenu.Trigger>
        {#snippet child({props})}
          <Button {...props} variant="secondary" size="sm" class="gap-2">
            {@render wsMark(selected)}
            <span class="truncate">{selected.name}</span>
            <span class="text-muted-foreground text-xs">{selected.abbreviation}</span>
            <Icon icon="i-mdi-chevron-down" class="size-4" />
          </Button>
        {/snippet}
      </ResponsiveMenu.Trigger>
      <ResponsiveMenu.Content>
        <div class="px-2 py-1.5 text-sm text-muted-foreground">{$t`Choose a language`}</div>
        {#each writingSystems as ws (ws.wsId)}
          <ResponsiveMenu.Item onSelect={() => onPick(ws.wsId)} class={ws.wsId === selected.wsId ? 'bg-muted' : undefined}>
            {@render wsMark(ws)}
            <span class="truncate">{ws.name}</span>
            <span class="text-muted-foreground ms-auto ps-2 text-xs">{ws.abbreviation}</span>
          </ResponsiveMenu.Item>
        {/each}
      </ResponsiveMenu.Content>
    </ResponsiveMenu.Root>
  </div>
{/snippet}

{#snippet taskRow(task: Task, ws: IWritingSystem | undefined, showWs: boolean)}
  {@const progress = stats.progress[task.id]}
  <li>
    <ListItem role={undefined} onclick={() => onSelect(task.id)}>
      <div class="flex w-full min-w-0 flex-col gap-2 sm:flex-row sm:items-center sm:gap-4">
        <div class="flex min-w-0 flex-col sm:flex-1">
          <div class="flex min-w-0 items-center gap-2">
            <span class="truncate font-medium">{task.fieldLabel}</span>
            {#if ws && showWs}
              <span class="flex shrink-0 items-center gap-1 text-xs {wsColor(ws)}" title={ws.name}>
                {@render wsMark(ws)}{ws.abbreviation}
              </span>
            {/if}
          </div>
          <span class="truncate text-sm text-muted-foreground">{task.prompt}</span>
        </div>
        <div class="flex min-w-0 items-center gap-3 sm:flex-1">
          {#if progress}
            {#if progress.remaining === 0}
              <span class="ms-auto text-sm text-muted-foreground">{$t`All done`}</span>
            {:else}
              {@const remaining = formatNumber(progress.remaining)}
              <Progress value={progress.percentDone} class="h-1.5 flex-1" aria-hidden="true" />
              <span class="shrink-0 text-sm tabular-nums text-muted-foreground">{$t`${remaining} to go`}</span>
            {/if}
          {:else}
            <Skeleton class="h-1.5 flex-1" />
            <Skeleton class="h-4 w-12 shrink-0" />
          {/if}
        </div>
      </div>
      {#snippet actions()}
        <Icon icon="i-mdi-chevron-right" class="text-muted-foreground shrink-0" />
      {/snippet}
    </ListItem>
  </li>
{/snippet}

<div class="flex flex-col gap-4">
  {#if vernacular.length > 1 || analysis.length > 1}
    <div class="flex flex-wrap gap-4">
      {#if vernacular.length > 1 && vernacularScope}
        {@render scopePicker($t`Vernacular`, vernacular, vernacularScope, wsId => void storage.taskVernacularWsId.set(wsId))}
      {/if}
      {#if analysis.length > 1 && analysisScope}
        {@render scopePicker($t`Analysis`, analysis, analysisScope, wsId => void storage.taskAnalysisWsId.set(wsId))}
      {/if}
    </div>
  {/if}

  {#if stats.totalEntries === 0}
    <div class="flex flex-col gap-1 px-4 py-8 text-center">
      <p class="text-lg font-medium">{$t`Your dictionary starts here`}</p>
      <p class="text-muted-foreground text-sm">{$t`Add your first entry to start building your dictionary.`}</p>
    </div>
  {:else if todo.length === 0 && done.length > 0}
    <p class="px-4 text-lg font-medium">{$t`Every task is complete`} 🎊</p>
  {/if}

  <ul class="flex flex-col gap-2">
    {#each stats.totalEntries === 0 ? [] : todo as {task, ws} (task.id)}
      {@render taskRow(task, ws, vernacular.length > 1 || analysis.length > 1)}
    {/each}
  </ul>

  {#if done.length > 0 && stats.totalEntries !== 0}
    <Collapsible.Root>
      <Collapsible.Trigger class="text-muted-foreground flex items-center gap-2 px-4 py-2 text-sm">
        {#snippet child({props})}
          <button {...props} class="text-muted-foreground hover:text-foreground flex items-center gap-2 px-4 py-2 text-sm">
            <Icon icon="i-mdi-check-circle-outline" />
            {$t`Nothing left to do`} ({formatNumber(done.length)})
            <Icon icon="i-mdi-chevron-down" class="transition-transform in-data-[state=open]:rotate-180" />
          </button>
        {/snippet}
      </Collapsible.Trigger>
      <Collapsible.Content>
        <ul class="mt-2 flex flex-col gap-2">
          {#each done as {task, ws} (task.id)}
            {@render taskRow(task, ws, vernacular.length > 1 || analysis.length > 1)}
          {/each}
        </ul>
      </Collapsible.Content>
    </Collapsible.Root>
  {/if}
</div>
