<script lang="ts">
  import ListItem from '$lib/components/ListItem.svelte';
  import {CircularProgress} from '$lib/components/ui/circular-progress';
  import {Icon} from '$lib/components/ui/icon';
  import {Button} from '$lib/components/ui/button';
  import {Skeleton} from '$lib/components/ui/skeleton';
  import {useWritingSystemService} from '$project/data';
  import {useFeatures} from '$lib/services/feature-service';
  import {type IWritingSystem, WritingSystemType} from '$lib/dotnet-types';
  import {plural, t} from 'svelte-i18n-lingui';
  import {watch} from 'runed';
  import {navigate, useRouter} from 'svelte-routing';
  import {taskLabel, useTasksService, type Task} from './tasks-service';
  import {getEntityConfig, type EntityType} from '$lib/views/entity-config';
  import {pt, tvt} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {useTasksStats} from './tasks-stats.svelte';

  let {onSelect, lastTaskId}: {onSelect: (taskId: string) => void, lastTaskId?: string} = $props();

  const tasksService = useTasksService();
  const writingSystemService = useWritingSystemService();
  const features = useFeatures();
  const viewService = useViewService();
  const {base} = useRouter();

  type Target = {task: Task, ws?: IWritingSystem};

  function writingSystemOf(task: Task): IWritingSystem | undefined {
    if (!task.subjectWritingSystemId) return undefined;
    const writingSystems = task.subjectWritingSystemType === WritingSystemType.Vernacular
      ? writingSystemService.vernacular
      : writingSystemService.analysis;
    return writingSystems.find(ws => ws.wsId === task.subjectWritingSystemId);
  }

  // Grouped by field so the row count doesn't grow with the writing systems.
  const fields = $derived.by(() => {
    const groups: {key: string, label: string, description?: string, entity: EntityType, targets: Target[]}[] = [];
    for (const task of tasksService.listTasks()) {
      const ws = writingSystemOf(task);
      // The editors hide audio writing systems when the feature is off, so there'd be nothing to fill in.
      if (ws?.isAudio && !features.audio) continue;
      const entity = task.subjectType === 'example-sentence' ? 'example' : task.subjectType;
      const key = `${entity}:${task.subjectFields.join()}`;
      let group = groups.find(g => g.key === key);
      if (!group) {
        group = {key, label: pt($tvt(taskLabel(task)), viewService.currentView), description: task.description && pt($tvt(task.description), viewService.currentView), entity, targets: []};
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

  const statsResource = useTasksStats();
  const stats = $derived(statsResource.current);
  watch(() => tasksService.listTasks().map(t => t.id).join(), () => void statsResource.refetch());

  function remainingText(remaining: number): string {
    return pt(
      $plural(remaining, {one: '# entry to go', other: '# entries to go'}),
      $plural(remaining, {one: '# word to go', other: '# words to go'}),
      viewService.currentView);
  }

  function wsColor(ws: IWritingSystem): string {
    return writingSystemService.wsColor(ws.wsId, ws.type === WritingSystemType.Vernacular ? 'vernacular' : 'analysis');
  }

  let list = $state<HTMLElement>();
  let restoredFocus = false;
  $effect(() => {
    if (restoredFocus || !lastTaskId || !list) return;
    restoredFocus = true;
    list.querySelector<HTMLElement>(`[data-task-id="${lastTaskId}"]`)?.focus();
  });
</script>

{#snippet progressAndName({task, ws}: Target, fieldLabel?: string)}
  {@const progress = stats.progress[task.id]}
  {@const remaining = progress ? remainingText(progress.remaining) : ''}
  {#if fieldLabel}<span class="sr-only">{fieldLabel},</span>{/if}
  {#if !progress}
    <Skeleton class="size-4 shrink-0 rounded-full" />
  {:else if progress.remaining === 0}
    <Icon icon="i-mdi-check-circle" class="size-4" />
  {:else}
    <CircularProgress value={progress.percentDone} size={16} strokeWidth={2.5} />
  {/if}
  {#if ws?.isAudio}
    <Icon icon="i-mdi-microphone" class="size-4" />
    <span class="sr-only">{$t`Audio`},</span>
  {/if}
  {#if ws}
    <span class="flex items-baseline gap-1.5">
      {ws.name}
      {#if ws.abbreviation}<span class="text-muted-foreground text-xs">{ws.abbreviation}</span>{/if}
    </span>
    {#if progress}<span class="sr-only">, {remaining}</span>{/if}
  {:else if progress}
    <!-- No language to name it by, so show the count instead. -->
    <span class="text-muted-foreground tabular-nums">{remaining}</span>
  {/if}
{/snippet}

{#snippet rowContent(label: string, description: string | undefined, targets: Target[], single: boolean)}
  <span class="flex flex-wrap items-baseline gap-x-2">
    <span class="font-medium">{label}</span>
    {#if description}<span class="text-muted-foreground text-sm">{description}</span>{/if}
  </span>
  <span class="mt-1 flex flex-wrap items-center gap-2">
    {#each targets as target (target.task.id)}
      {@const progress = stats.progress[target.task.id]}
      {@const name = target.ws && `${target.ws.name} (${target.ws.wsId})`}
      {@const title = name && (progress ? `${name}: ${remainingText(progress.remaining)}` : name)}
      {@const classes = `flex min-h-8 items-center gap-1.5 rounded-full text-sm ${target.ws ? wsColor(target.ws) : ''}`}
      {#if single}
        <span class={classes} {title}>{@render progressAndName(target)}</span>
      {:else}
        <button
          type="button"
          class="{classes} bg-background/60 hover:bg-primary/15 focus-visible:ring-ring/50 dark:hover:bg-primary/25 px-2.5 shadow-sm transition-colors outline-none focus-visible:ring-[3px]"
          {title}
          onclick={() => onSelect(target.task.id)}
          data-task-id={target.task.id}
        >
          {@render progressAndName(target, label)}
        </button>
      {/if}
    {/each}
  </span>
{/snippet}

{#snippet fieldRows(rows: typeof fields)}
  <div class="flex flex-col gap-2">
    {#each rows as {key, label, description, targets} (key)}
      {#if targets.length === 1}
        <ListItem onclick={() => onSelect(targets[0].task.id)} data-task-id={targets[0].task.id}>
          {@render rowContent(label, description, targets, true)}
          {#snippet actions()}
            <Icon icon="i-mdi-chevron-right" class="text-muted-foreground shrink-0" />
          {/snippet}
        </ListItem>
      {:else}
        <ListItem element="div">
          {@render rowContent(label, description, targets, false)}
        </ListItem>
      {/if}
    {/each}
  </div>
{/snippet}

{#if stats.totalEntries === 0}
  <div class="flex flex-col items-start gap-3 px-4">
    <p class="text-muted-foreground">{pt($t`Add some entries first.`, $t`Add some words first.`, viewService.currentView)}</p>
    <Button variant="outline" icon="i-mdi-book-alphabet" onclick={() => navigate(`${$base.uri}/browse`)}>{$t`Browse`}</Button>
  </div>
{:else if entities.length === 0}
  <p class="text-muted-foreground px-4">{$t`No tasks right now.`}</p>
{:else}
  <div class="flex flex-col gap-6" bind:this={list}>
    {#each entities as {entity, label, fields: rows} (entity)}
      {#if entities.length > 1}
        <section class="flex flex-col gap-2" aria-labelledby="task-entity-{entity}">
          <h2 id="task-entity-{entity}" class="text-muted-foreground px-4 text-sm font-medium">{label}</h2>
          {@render fieldRows(rows)}
        </section>
      {:else}
        {@render fieldRows(rows)}
      {/if}
    {/each}
  </div>
{/if}
