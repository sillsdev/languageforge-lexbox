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

  function chosen(writingSystems: IWritingSystem[], stored: string): IWritingSystem | undefined {
    return writingSystems.find(ws => ws.wsId === stored)
      ?? writingSystems.find(ws => !ws.isAudio)
      ?? writingSystems[0];
  }

  type Group = {
    key: string;
    writingSystem?: IWritingSystem;
    options: IWritingSystem[];
    choose: (wsId: string) => void;
    tasks: Task[];
  };

  const groups = $derived.by<Group[]>(() => {
    const tasks = tasksService.listTasks();
    function family(key: string, options: IWritingSystem[], stored: string, choose: (wsId: string) => void, type: WritingSystemType): Group {
      const writingSystem = chosen(options, stored);
      return {
        key,
        writingSystem,
        options,
        choose,
        tasks: tasks.filter(task => task.subjectWritingSystemType === type && task.subjectWritingSystemId === writingSystem?.wsId),
      };
    }
    return [
      family('vernacular', vernacular, storage.taskVernacularWsId.current, wsId => void storage.taskVernacularWsId.set(wsId), WritingSystemType.Vernacular),
      family('analysis', analysis, storage.taskAnalysisWsId.current, wsId => void storage.taskAnalysisWsId.set(wsId), WritingSystemType.Analysis),
      {key: 'any', options: [], choose: () => {}, tasks: tasks.filter(task => !task.subjectWritingSystemId)},
    ].filter(group => group.tasks.length > 0);
  });

  // Headers only earn their place when the project gives you a language to choose.
  const grouped = $derived(groups.some(group => group.options.length > 1));
  const rows = $derived(groups.flatMap(group => group.tasks.map(task => ({task, ws: group.writingSystem}))));

  // Hooks must run while the component initialises, so the resource takes a getter and
  // reads the rows later, on load.
  const statsResource = useTasksStats(() => rows.map(row => row.task));
  const stats = $derived(statsResource.current);
  // Switching language swaps in tasks the counts were never loaded for.
  watch(() => rows.map(row => row.task.id).join(), () => void statsResource.refetch());

  const done = $derived(rows.filter(row => stats.progress[row.task.id]?.remaining === 0));
  function todoOf(group: Group): Task[] {
    return group.tasks.filter(task => stats.progress[task.id]?.remaining !== 0);
  }

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

{#snippet groupHeading(group: Group)}
  {@const ws = group.writingSystem}
  {#if group.options.length > 1 && ws}
    <h2 id="task-group-{group.key}" class="ps-2">
      <ResponsiveMenu.Root>
        <ResponsiveMenu.Trigger>
          {#snippet child({props})}
            <Button {...props} variant="ghost" size="sm" class="max-w-full gap-2">
              {@render wsMark(ws)}
              <span class="truncate font-medium">{ws.name}</span>
              <span class="text-muted-foreground text-xs">{ws.abbreviation}</span>
              <Icon icon="i-mdi-chevron-down" class="size-4 shrink-0" />
            </Button>
          {/snippet}
        </ResponsiveMenu.Trigger>
        <ResponsiveMenu.Content>
          <div class="text-muted-foreground px-2 py-1.5 text-sm">{$t`Choose a language`}</div>
          {#each group.options as option (option.wsId)}
            <ResponsiveMenu.Item onSelect={() => group.choose(option.wsId)} class={option.wsId === ws.wsId ? 'bg-muted' : undefined}>
              {@render wsMark(option)}
              <span class="truncate">{option.name}</span>
              <span class="text-muted-foreground ms-auto ps-2 text-xs">{option.abbreviation}</span>
            </ResponsiveMenu.Item>
          {/each}
        </ResponsiveMenu.Content>
      </ResponsiveMenu.Root>
    </h2>
  {:else}
    <h2 id="task-group-{group.key}" class="text-muted-foreground flex items-center gap-2 ps-5 pe-4 text-sm font-medium">
      {#if ws}
        {@render wsMark(ws)}{ws.name}<span class="text-xs">{ws.abbreviation}</span>
      {:else}
        {$t`Same in every language`}
      {/if}
    </h2>
  {/if}
{/snippet}

{#snippet taskRow(task: Task, ws: IWritingSystem | undefined, showWs: boolean)}
  {@const progress = stats.progress[task.id]}
  {@const remaining = progress ? formatNumber(progress.remaining) : ''}
  {@const field = task.fieldLabel}
  <li>
    <ListItem role={undefined} onclick={() => onSelect(task.id)}
              aria-label={!progress ? undefined : ws && showWs
                ? $t`${field} in ${ws.name}: ${remaining} to go`
                : $t`${field}: ${remaining} to go`}>
      <div class="flex w-full min-w-0 flex-col gap-2 sm:flex-row sm:items-center sm:gap-4">
        <div class="flex min-w-0 flex-col sm:flex-1">
          <div class="flex min-w-0 items-center gap-2">
            <span class="truncate font-medium">{field}</span>
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
              <Progress value={progress.percentDone} class="h-1.5 flex-1" aria-hidden="true" />
              <span class="shrink-0 text-sm tabular-nums text-muted-foreground" aria-hidden="true">{$t`${remaining} to go`}</span>
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

<div class="flex flex-col gap-6">
  {#if stats.totalEntries === 0}
    <div class="flex flex-col gap-1 px-4 py-8 text-center">
      <p class="text-lg font-medium">{$t`Your dictionary starts here`}</p>
      <p class="text-muted-foreground text-sm">{$t`Add your first entry to start building your dictionary.`}</p>
    </div>
  {:else}
    {#if done.length === rows.length && rows.length > 0}
      <p class="px-4 text-lg font-medium">{$t`Every task is complete`} 🎊</p>
    {/if}

    {#if grouped}
      {#each groups as group (group.key)}
        {@const todo = todoOf(group)}
        {@const switchable = group.options.length > 1}
        <!-- A finished group keeps its heading while there are other languages to switch to. -->
        {#if todo.length > 0 || switchable}
          <section class="flex flex-col gap-2" aria-labelledby="task-group-{group.key}">
            {@render groupHeading(group)}
            {#if todo.length > 0}
              <ul class="flex flex-col gap-2">
                {#each todo as task (task.id)}
                  {@render taskRow(task, group.writingSystem, false)}
                {/each}
              </ul>
            {:else if group.writingSystem}
              {@const language = group.writingSystem.name}
              <p class="text-muted-foreground ps-5 pe-4 text-sm">{$t`All done in ${language}`}</p>
            {/if}
          </section>
        {/if}
      {/each}
    {:else}
      <ul class="flex flex-col gap-2">
        {#each rows.filter(row => stats.progress[row.task.id]?.remaining !== 0) as {task, ws} (task.id)}
          {@render taskRow(task, ws, false)}
        {/each}
      </ul>
    {/if}

    {#if done.length > 0}
      <Collapsible.Root>
        <Collapsible.Trigger>
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
              {@render taskRow(task, ws, grouped)}
            {/each}
          </ul>
        </Collapsible.Content>
      </Collapsible.Root>
    {/if}
  {/if}
</div>
