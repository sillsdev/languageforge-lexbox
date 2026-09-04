<script lang="ts">
  import ListItem from '$lib/components/ListItem.svelte';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {CircularProgress} from '$lib/components/ui/circular-progress';
  import {Icon} from '$lib/components/ui/icon';
  import {formatNumber} from '$lib/components/ui/format';
  import {useWritingSystemService} from '$project/data';
  import {useFeatures} from '$lib/services/feature-service';
  import {type IWritingSystem, WritingSystemType} from '$lib/dotnet-types';
  import {t} from 'svelte-i18n-lingui';
  import {watch} from 'runed';
  import {useTasksService, type Task} from './tasks-service';
  import {getEntityConfig, type EntityType} from '$lib/views/entity-config';
  import {pt, tvt} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {useTasksStats} from './tasks-stats.svelte';

  let {onSelect}: {onSelect: (taskId: string) => void} = $props();

  const tasksService = useTasksService();
  const writingSystemService = useWritingSystemService();
  const features = useFeatures();
  const viewService = useViewService();

  type Target = {task: Task, ws?: IWritingSystem};

  function writingSystemOf(task: Task): IWritingSystem | undefined {
    if (!task.subjectWritingSystemId) return undefined;
    const writingSystems = task.subjectWritingSystemType === WritingSystemType.Vernacular
      ? writingSystemService.vernacular
      : writingSystemService.analysis;
    return writingSystems.find(ws => ws.wsId === task.subjectWritingSystemId);
  }

  // One row per field, with a target per writing system, so the row count stays the same
  // no matter how many writing systems the project has.
  const fields = $derived.by(() => {
    const groups: {label: string, entity: EntityType, targets: Target[]}[] = [];
    for (const task of tasksService.listTasks()) {
      const ws = writingSystemOf(task);
      // The editors hide audio writing systems when the feature is off, so those tasks
      // would open with nothing to fill in.
      if (ws?.isAudio && !features.audio) continue;
      let group = groups.find(g => g.label === task.fieldLabel);
      if (!group) {
        group = {label: task.fieldLabel, entity: task.subjectType === 'example-sentence' ? 'example' : task.subjectType, targets: []};
        groups.push(group);
      }
      group.targets.push({task, ws});
    }
    for (const group of groups) {
      group.targets.sort((a, b) => Number(a.ws?.isAudio ?? false) - Number(b.ws?.isAudio ?? false));
    }
    return groups.filter(group => group.targets.length > 0);
  });

  const entities = $derived((['entry', 'sense', 'example'] as const)
    .map(entity => ({
      entity,
      label: pt($tvt(getEntityConfig(entity).$label), viewService.currentView),
      fields: fields.filter(field => field.entity === entity),
    }))
    .filter(group => group.fields.length > 0));

  // Hooks must run while the component initialises, so the resource takes a getter and
  // reads the fields later, on load.
  const statsResource = useTasksStats(() => fields.flatMap(field => field.targets.map(target => target.task)));
  const stats = $derived(statsResource.current);
  watch(() => fields.flatMap(f => f.targets.map(t => t.task.id)).join(), () => void statsResource.refetch());

  function wsColor(ws: IWritingSystem): string {
    return writingSystemService.wsColor(ws.wsId, ws.type === WritingSystemType.Vernacular ? 'vernacular' : 'analysis');
  }
</script>

{#snippet writingSystemProgress({task, ws}: Target)}
  {@const progress = stats.progress[task.id]}
  {@const remaining = progress ? formatNumber(progress.remaining) : ''}
  <span class="flex items-center gap-1 text-sm {ws ? wsColor(ws) : 'text-muted-foreground'}"
        title={ws ? (progress ? `${ws.name}: ${$t`${remaining} to go`}` : ws.name) : (progress ? $t`${remaining} to go` : undefined)}>
    <span aria-hidden="true" class="flex">
      <CircularProgress value={progress?.percentDone ?? 0} size={16} strokeWidth={2.5} />
    </span>
    {#if ws?.isAudio}
      <Icon icon="i-mdi-microphone" class="size-4" />
      <span class="sr-only">{$t`Audio`}</span>
    {/if}
    {#if ws}
      <span>{ws.abbreviation}</span>
    {:else if progress}
      <!-- Nothing to name a language-free task by, so say how much of it is left. -->
      <span class="tabular-nums">{$t`${remaining} to go`}</span>
    {/if}
  </span>
{/snippet}

{#snippet fieldRow(label: string, targets: Target[], props?: Record<string, unknown>)}
  <ListItem {...props} role="listitem">
    <span class="truncate font-medium">{label}</span>
    <!-- Second line so every writing system stays visible, even on a phone. -->
    <span class="mt-1.5 flex flex-wrap items-center gap-x-3 gap-y-1.5">
      {#each targets as target (target.task.id)}
        {@render writingSystemProgress(target)}
      {/each}
    </span>
    {#snippet actions()}
      <Icon icon={targets.length === 1 ? 'i-mdi-chevron-right' : 'i-mdi-chevron-down'} class="text-muted-foreground shrink-0" />
    {/snippet}
  </ListItem>
{/snippet}

{#snippet fieldRows(rows: typeof fields)}
  <div class="flex flex-col gap-2" role="list">
    {#each rows as {label, targets} (label)}
      {#if targets.length === 1}
        {@render fieldRow(label, targets, {onclick: () => onSelect(targets[0].task.id)})}
      {:else}
        <ResponsiveMenu.Root>
          <ResponsiveMenu.Trigger>
            {#snippet child({props})}
              {@render fieldRow(label, targets, props)}
            {/snippet}
          </ResponsiveMenu.Trigger>
          <ResponsiveMenu.Content>
            <div class="px-2 py-1.5 text-sm text-muted-foreground">{$t`Choose a language`}</div>
            {#each targets as {task, ws} (task.id)}
              {@const progress = stats.progress[task.id]}
              {@const remaining = progress ? formatNumber(progress.remaining) : ''}
              <ResponsiveMenu.Item onSelect={() => onSelect(task.id)}>
                <span aria-hidden="true" class="flex {wsColor(ws!)}"><CircularProgress value={progress?.percentDone ?? 0} size={16} strokeWidth={2.5} /></span>
                {#if ws!.isAudio}<Icon icon="i-mdi-microphone" class="size-4 {wsColor(ws!)}" />{/if}
                <span class="truncate">{ws!.name}</span>
                {#if progress}
                  <span class="text-muted-foreground ms-auto ps-2 text-xs tabular-nums">{$t`${remaining} to go`}</span>
                {:else}
                  <span class="text-muted-foreground ms-auto ps-2 text-xs">{ws!.abbreviation}</span>
                {/if}
              </ResponsiveMenu.Item>
            {/each}
          </ResponsiveMenu.Content>
        </ResponsiveMenu.Root>
      {/if}
    {/each}
  </div>
{/snippet}

<div class="flex max-w-2xl flex-col gap-6">
  {#each entities as {entity, label, fields: rows} (entity)}
    <section class="flex flex-col gap-2" aria-labelledby="task-entity-{entity}">
      <h2 id="task-entity-{entity}" class="text-muted-foreground px-4 text-sm font-medium">{label}</h2>
      {@render fieldRows(rows)}
    </section>
  {/each}
</div>
