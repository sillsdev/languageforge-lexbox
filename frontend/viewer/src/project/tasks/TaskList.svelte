<script lang="ts">
  import ListItem from '$lib/components/ListItem.svelte';
  import {CircularProgress} from '$lib/components/ui/circular-progress';
  import {Icon} from '$lib/components/ui/icon';
  import {Button} from '$lib/components/ui/button';
  import {Skeleton} from '$lib/components/ui/skeleton';
  import {formatNumber} from '$lib/components/ui/format';
  import {useWritingSystemService} from '$project/data';
  import {useFeatures} from '$lib/services/feature-service';
  import {type IWritingSystem, WritingSystemType} from '$lib/dotnet-types';
  import {t} from 'svelte-i18n-lingui';
  import {watch} from 'runed';
  import {navigate, useRouter} from 'svelte-routing';
  import {useTasksService, type Task} from './tasks-service';
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

  const languageButtons: Record<string, HTMLElement> = {};
  let restoredFocus = false;
  // Coming back from a task should leave you where you were, not at the top of the page.
  $effect(() => {
    if (restoredFocus || !lastTaskId || fields.length === 0) return;
    restoredFocus = true;
    languageButtons[lastTaskId]?.focus();
  });
</script>

{#snippet progressAndName(fieldLabel: string, {task, ws}: Target)}
  {@const progress = stats.progress[task.id]}
  {@const remaining = progress ? formatNumber(progress.remaining) : ''}
  {#if !progress}
    <Skeleton class="size-4 shrink-0 rounded-full" />
  {:else if progress.remaining === 0}
    <Icon icon="i-mdi-check-circle" class="size-4" />
  {:else}
    <CircularProgress value={progress.percentDone} size={16} strokeWidth={2.5} />
  {/if}
  {#if ws?.isAudio}
    <Icon icon="i-mdi-microphone" class="size-4" />
  {/if}
  {#if ws}
    <span aria-hidden="true">{ws.abbreviation}</span>
  {:else if progress}
    <!-- Nothing to name a language-free task by, so say how much of it is left. -->
    <span class="tabular-nums">{$t`${remaining} to go`}</span>
  {/if}
  <span class="sr-only">
    {fieldLabel}{#if ws}, {ws.name}{/if}{#if ws?.isAudio}, {$t`Audio`}{/if}{#if progress}, {$t`${remaining} to go`}{/if}
  </span>
{/snippet}

{#snippet rowContent(label: string, targets: Target[], single: boolean)}
  <span class="truncate font-medium">{label}</span>
  <!-- Second line so every language stays visible, even on a phone. -->
  <span class="mt-1 flex flex-wrap items-center gap-2">
    {#each targets as target (target.task.id)}
      {@const progress = stats.progress[target.task.id]}
      {@const remaining = progress ? formatNumber(progress.remaining) : ''}
      {@const title = target.ws && (progress ? `${target.ws.name}: ${$t`${remaining} to go`}` : target.ws.name)}
      {@const classes = `flex min-h-8 items-center gap-1 rounded-full text-sm ${target.ws ? wsColor(target.ws) : 'text-muted-foreground'}`}
      {#if single}
        <span class={classes} {title}>{@render progressAndName(label, target)}</span>
      {:else}
        <!-- Every language is already on screen, so pick one here rather than through a menu. -->
        <button
          type="button"
          class="{classes} bg-background/60 hover:bg-primary/15 focus-visible:ring-ring/50 dark:hover:bg-primary/25 px-2.5 shadow-sm transition-colors outline-none focus-visible:ring-[3px]"
          {title}
          onclick={() => onSelect(target.task.id)}
          bind:this={languageButtons[target.task.id]}
        >
          {@render progressAndName(label, target)}
        </button>
      {/if}
    {/each}
  </span>
{/snippet}

{#snippet fieldRows(rows: typeof fields)}
  <div class="flex flex-col gap-2">
    {#each rows as {label, targets} (label)}
      {#if targets.length === 1}
        <ListItem onclick={() => onSelect(targets[0].task.id)} bind:ref={languageButtons[targets[0].task.id]}>
          {@render rowContent(label, targets, true)}
          {#snippet actions()}
            <Icon icon="i-mdi-chevron-right" class="text-muted-foreground shrink-0" />
          {/snippet}
        </ListItem>
      {:else}
        <ListItem element="div">
          {@render rowContent(label, targets, false)}
        </ListItem>
      {/if}
    {/each}
  </div>
{/snippet}

{#if stats.totalEntries === 0}
  <div class="flex flex-col items-start gap-3 px-4">
    <p class="text-muted-foreground">{$t`Tasks help you fill in what's missing, so add some entries first.`}</p>
    <Button variant="outline" icon="i-mdi-book-alphabet" onclick={() => navigate(`${$base.uri}/browse`)}>{$t`Browse`}</Button>
  </div>
{:else if entities.length === 0}
  <p class="text-muted-foreground px-4">{$t`No tasks right now.`}</p>
{:else}
  <div class="flex flex-col gap-6">
    {#each entities as {entity, label, fields: rows} (entity)}
      <!-- With one entity there is nothing to tell its heading apart from, so drop it. -->
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
