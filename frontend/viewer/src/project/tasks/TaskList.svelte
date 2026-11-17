<script lang="ts">
  import ListItem from '$lib/components/ListItem.svelte';
  import * as Collapsible from '$lib/components/ui/collapsible';
  import * as Select from '$lib/components/ui/select';
  import {Icon} from '$lib/components/ui/icon';
  import {Button} from '$lib/components/ui/button';
  import {Skeleton} from '$lib/components/ui/skeleton';
  import {useWritingSystemService} from '$project/data';
  import {useFeatures} from '$lib/services/feature-service';
  import {type IWritingSystem, WritingSystemType} from '$lib/dotnet-types';
  import {plural, t} from 'svelte-i18n-lingui';
  import {navigate, useRouter} from 'svelte-routing';
  import {taskLabel, useTasksService, type Task} from './tasks-service';
  import {getEntityConfig} from '$lib/views/entity-config';
  import {pt, tvt} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {QueryParamState} from '$lib/utils/url.svelte';
  import {useTasksStats} from './tasks-stats.svelte';
  import {tick} from 'svelte';
  import {watch} from 'runed';

  let {onSelect, lastTaskId}: {onSelect: (taskId: string) => void, lastTaskId?: string} = $props();

  const tasksService = useTasksService();
  const writingSystemService = useWritingSystemService();
  const features = useFeatures();
  const viewService = useViewService();
  const {base} = useRouter();

  type Row = {task: Task, entity: 'entry' | 'sense' | 'example', ws?: IWritingSystem};

  const wsFilter = new QueryParamState({key: 'ws', replaceOnDefaultValue: true});
  // The editors hide audio writing systems when the feature is off, so there'd be nothing to fill in.
  function usable(ws: IWritingSystem) {
    return !ws.isAudio || features.audio;
  }
  function wsKey(ws: IWritingSystem) {
    return `${ws.type}:${ws.wsId}`;
  }
  const filterGroups = $derived([
    {label: $t`Vernacular`, writingSystems: writingSystemService.vernacular.filter(usable)},
    {label: $t`Analysis`, writingSystems: writingSystemService.analysis.filter(usable)},
  ].filter(group => group.writingSystems.length));
  const filterWs = $derived(filterGroups.flatMap(g => g.writingSystems).find(ws => wsKey(ws) === wsFilter.current));

  function writingSystemOf(task: Task): IWritingSystem | undefined {
    if (!task.subjectWritingSystemId) return undefined;
    const writingSystems = task.subjectWritingSystemType === WritingSystemType.Vernacular
      ? writingSystemService.vernacular
      : writingSystemService.analysis;
    return writingSystems.find(ws => ws.wsId === task.subjectWritingSystemId);
  }

  function fieldKey(task: Task) {
    return `${task.subjectType}:${task.subjectFields.join()}`;
  }
  const rows = $derived.by(() => {
    const tasks = tasksService.listTasks();
    // The service lists tasks per writing system; show each field's writing systems together instead.
    const fieldOrder = [...new Set(tasks.map(fieldKey))];
    return tasks
      .map((task): Row => ({task, entity: task.subjectType === 'example-sentence' ? 'example' : task.subjectType, ws: writingSystemOf(task)}))
      .filter(row => !row.ws || usable(row.ws))
      // Tasks without a writing system belong to no language, so a language filter hides them.
      .filter(row => !filterWs || (row.ws && wsKey(row.ws) === wsKey(filterWs)))
      .sort((a, b) => fieldOrder.indexOf(fieldKey(a.task)) - fieldOrder.indexOf(fieldKey(b.task))
        || Number(a.ws?.isAudio ?? false) - Number(b.ws?.isAudio ?? false));
  });

  const stats = $derived(useTasksStats().current);
  function isDone(row: Row) {
    return stats.progress[row.task.id]?.remaining === 0;
  }
  const openRows = $derived(rows.filter(row => !isDone(row)));
  const doneRows = $derived(rows.filter(isDone));

  const entities = $derived((['entry', 'sense', 'example'] as const)
    .map(entity => ({
      entity,
      label: pt($tvt(getEntityConfig(entity).$label), viewService.currentView),
      rows: openRows.filter(row => row.entity === entity),
    }))
    .filter(group => group.rows.length > 0));

  let doneOpen = $state(false);
  let container = $state<HTMLElement>();
  let doneTrigger = $state<HTMLElement | null>(null);
  watch(() => lastTaskId, () => {
    if (lastTaskId) void tick().then(() => focusRow(lastTaskId));
  });
  function focusRow(taskId: string) {
    const row = container?.querySelector<HTMLElement>(`[data-task-id="${taskId}"]`);
    (row?.checkVisibility() ? row : doneTrigger)?.focus();
  }

  function remainingText(remaining: number): string {
    return pt(
      $plural(remaining, {one: '# entry to go', other: '# entries to go'}),
      $plural(remaining, {one: '# word to go', other: '# words to go'}),
      viewService.currentView);
  }

  function wsColor(ws: IWritingSystem): string {
    return writingSystemService.wsColor(ws.wsId, ws.type === WritingSystemType.Vernacular ? 'vernacular' : 'analysis');
  }
</script>

{#snippet wsName(ws: IWritingSystem)}
  <span class="inline-flex items-baseline gap-1.5 {wsColor(ws)}">
    {ws.name}
    {#if ws.abbreviation}<span class="text-muted-foreground text-xs">{ws.abbreviation}</span>{/if}
    {#if ws.isAudio}<Icon icon="i-mdi-microphone" class="size-4 self-center" /><span class="sr-only">{$t`Audio`}</span>{/if}
  </span>
{/snippet}

{#snippet taskRow({task, ws}: Row, done: boolean)}
  {@const progress = stats.progress[task.id]}
  {@const description = task.description && pt($tvt(task.description), viewService.currentView)}
  <li>
    <ListItem
      role={undefined}
      data-task-id={task.id}
      icon={done ? 'i-mdi-check-circle-outline' : undefined}
      onclick={() => onSelect(task.id)}>
      <span class="flex flex-wrap items-baseline gap-x-2 font-medium">
        {pt($tvt(taskLabel(task)), viewService.currentView)}
        {#if ws && !filterWs}<span aria-hidden="true" class="text-muted-foreground">·</span>{@render wsName(ws)}{/if}
      </span>
      <span class="text-muted-foreground text-sm tabular-nums">
        {#if done}
          {$t`Done`}
        {:else if progress}
          {remainingText(progress.remaining)}
        {:else}
          <Skeleton class="h-4 w-24 my-0.5" />
        {/if}
        {#if description}<span aria-hidden="true">·</span> {description}{/if}
      </span>
      {#snippet actions()}
        <Icon icon="i-mdi-chevron-right" class="text-muted-foreground size-5 shrink-0" aria-hidden="true" />
      {/snippet}
    </ListItem>
  </li>
{/snippet}

{#if stats.totalEntries === 0}
  <div class="flex flex-col items-start gap-3 px-4">
    <p class="text-muted-foreground">{pt($t`Add some entries first.`, $t`Add some words first.`, viewService.currentView)}</p>
    <Button variant="outline" icon="i-mdi-book-alphabet" onclick={() => navigate(`${$base.uri}/browse`)}>{$t`Browse`}</Button>
  </div>
{:else}
  <div class="flex max-w-2xl flex-col gap-5" bind:this={container}>
    <p class="text-muted-foreground text-sm">{$t`Fill in what's missing, one field at a time.`}</p>
    <Select.Root type="single" bind:value={wsFilter.current}>
      <Select.Trigger class="w-full sm:w-auto sm:min-w-56 sm:self-start" aria-label={$t`Writing system`}>
        {#if filterWs}{@render wsName(filterWs)}{:else}{$t`All writing systems`}{/if}
      </Select.Trigger>
      <Select.Content>
        <Select.Item value="">{$t`All writing systems`}</Select.Item>
        {#each filterGroups as group (group.label)}
          <Select.Group>
            <Select.GroupHeading>{group.label}</Select.GroupHeading>
            {#each group.writingSystems as ws (wsKey(ws))}
              <Select.Item value={wsKey(ws)} label={ws.name}>{@render wsName(ws)}</Select.Item>
            {/each}
          </Select.Group>
        {/each}
      </Select.Content>
    </Select.Root>

    {#each entities as {entity, label, rows: entityRows} (entity)}
      <section class="flex flex-col gap-2">
        <h2 class="text-muted-foreground text-sm font-medium">{label}</h2>
        <ul class="flex flex-col gap-2">
          {#each entityRows as row (row.task.id)}
            {@render taskRow(row, false)}
          {/each}
        </ul>
      </section>
    {:else}
      <p>{filterWs ? $t`Nothing left to do for ${filterWs.name}.` : $t`Nothing left to do.`}</p>
    {/each}

    {#if doneRows.length}
      <Collapsible.Root open={doneOpen || openRows.length === 0} onOpenChange={open => doneOpen = open}>
        <Collapsible.Trigger>
          {#snippet child({props})}
            <Button {...props} bind:ref={doneTrigger} variant="ghost" class="group -ms-3" icon="i-mdi-chevron-right" iconProps={{class: 'transition-transform group-data-[state=open]:rotate-90'}}>
              {$t`Done (${doneRows.length})`}
            </Button>
          {/snippet}
        </Collapsible.Trigger>
        <Collapsible.Content>
          <ul class="flex flex-col gap-2 pt-2">
            {#each doneRows as row (row.task.id)}
              {@render taskRow(row, true)}
            {/each}
          </ul>
        </Collapsible.Content>
      </Collapsible.Root>
    {/if}
  </div>
{/if}
