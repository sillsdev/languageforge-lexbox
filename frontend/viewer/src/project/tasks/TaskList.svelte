<script lang="ts">
  import * as Card from '$lib/components/ui/card';
  import {CircularProgress} from '$lib/components/ui/circular-progress';
  import {Icon} from '$lib/components/ui/icon';
  import {Button} from '$lib/components/ui/button';
  import {Skeleton} from '$lib/components/ui/skeleton';
  import {useWritingSystemService} from '$project/data';
  import {useFeatures} from '$lib/services/feature-service';
  import {type IWritingSystem, WritingSystemType} from '$lib/dotnet-types';
  import {plural, t} from 'svelte-i18n-lingui';
  import {navigate, useRouter} from 'svelte-routing';
  import {taskLabel, useTasksService, type Task} from './tasks-service';
  import {getEntityConfig, type EntityType} from '$lib/views/entity-config';
  import {pt, tvt} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {useTasksStats} from './tasks-stats.svelte';

  let {onSelect}: {onSelect: (taskId: string) => void} = $props();

  const tasksService = useTasksService();
  const writingSystemService = useWritingSystemService();
  const features = useFeatures();
  const viewService = useViewService();
  const {base} = useRouter();

  type Target = {task: Task, ws?: IWritingSystem};
  type Field = {key: string, label: string, description?: string, entity: EntityType, targets: Target[]};

  function writingSystemOf(task: Task): IWritingSystem | undefined {
    if (!task.subjectWritingSystemId) return undefined;
    const writingSystems = task.subjectWritingSystemType === WritingSystemType.Vernacular
      ? writingSystemService.vernacular
      : writingSystemService.analysis;
    return writingSystems.find(ws => ws.wsId === task.subjectWritingSystemId);
  }

  // Grouped by field so the chip count grows with the writing systems, not the row count.
  const fields = $derived.by(() => {
    const groups: Field[] = [];
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

  const stats = $derived(useTasksStats().current);

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

{#snippet progressAndName({task, ws}: Target, fieldLabel: string)}
  {@const progress = stats.progress[task.id]}
  {@const remaining = progress ? remainingText(progress.remaining) : ''}
  <span class="sr-only">{fieldLabel},</span>
  {#if !progress}
    <Skeleton class="size-4 shrink-0 rounded-full" />
  {:else if progress.remaining === 0}
    <Icon icon="i-mdi-check-circle-outline" class="size-5" />
    <span class="sr-only">{$t`Complete`},</span>
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

{#snippet field({label, description, targets}: Field)}
  <div class="flex flex-col gap-x-3 gap-y-2">
    <span class="flex flex-wrap items-baseline gap-x-2">
      <span class="text-sm font-medium">{label}</span>
      {#if description}<span class="text-muted-foreground text-sm">{description}</span>{/if}
    </span>
    <span class="flex flex-wrap items-center gap-2">
      {#each targets as target (target.task.id)}
        {@const progress = stats.progress[target.task.id]}
        {@const name = target.ws && `${target.ws.name} (${target.ws.wsId})`}
        {@const title = name && (progress ? `${name}: ${remainingText(progress.remaining)}` : name)}
        <button
          type="button"
          class="bg-secondary hover:bg-secondary/80 focus-visible:ring-ring/50 inline-flex min-h-8 items-center gap-1.5 rounded-full px-3 text-sm font-medium shadow-sm transition-colors outline-hidden focus-visible:ring-[3px] {progress?.remaining === 0 ? 'opacity-60' : ''} {target.ws ? wsColor(target.ws) : ''}"
          {title}
          onclick={() => onSelect(target.task.id)}
        >
          {@render progressAndName(target, label)}
        </button>
      {/each}
    </span>
  </div>
{/snippet}

{#if stats.totalEntries === 0}
  <div class="flex flex-col items-start gap-3 px-4">
    <p class="text-muted-foreground">{pt($t`Add some entries first.`, $t`Add some words first.`, viewService.currentView)}</p>
    <Button variant="outline" icon="i-mdi-book-alphabet" onclick={() => navigate(`${$base.uri}/browse`)}>{$t`Browse`}</Button>
  </div>
{:else}
  <div class="flex w-fit flex-col gap-4">
    {#each entities as {entity, label, fields: rows} (entity)}
      <Card.Root class="gap-y-3 py-4">
        <Card.Header>
          <Card.Title>{label}</Card.Title>
        </Card.Header>
        <Card.Content class="flex flex-col gap-3">
          {#each rows as row (row.key)}
            {@render field(row)}
          {/each}
        </Card.Content>
      </Card.Root>
    {/each}
  </div>
{/if}
