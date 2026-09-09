<script lang="ts">
  import ListItem from '$lib/components/ListItem.svelte';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
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
  type Field = {key: string, label: string, targets: Target[]};

  function writingSystemOf(task: Task): IWritingSystem | undefined {
    if (!task.subjectWritingSystemId) return undefined;
    const writingSystems = task.subjectWritingSystemType === WritingSystemType.Vernacular
      ? writingSystemService.vernacular
      : writingSystemService.analysis;
    return writingSystems.find(ws => ws.wsId === task.subjectWritingSystemId);
  }

  // One row per field, with a target per writing system, so the row count stays the same
  // no matter how many writing systems the project has. Ordered by entity (entry, sense, example).
  const fields = $derived.by(() => {
    const order = {entry: 0, sense: 1, 'example-sentence': 2};
    const byKey: Record<string, Field & {sort: number}> = {};
    const list: (Field & {sort: number})[] = [];
    for (const task of tasksService.listTasks()) {
      const ws = writingSystemOf(task);
      // The editors hide audio writing systems when the feature is off, so those tasks
      // would open with nothing to fill in.
      if (ws?.isAudio && !features.audio) continue;
      const key = `${task.subjectType}:${task.subjectFields.join()}`;
      let field = byKey[key];
      if (!field) {
        field = {key, sort: order[task.subjectType], label: pt($tvt(taskLabel(task)), viewService.currentView), targets: []};
        byKey[key] = field;
        list.push(field);
      }
      field.targets.push({task, ws});
    }
    for (const field of list) field.targets.sort((a, b) => Number(a.ws?.isAudio ?? false) - Number(b.ws?.isAudio ?? false));
    return list.filter(f => f.targets.length).sort((a, b) => a.sort - b.sort);
  });

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

  function containsLast(field: Field): boolean {
    return !!lastTaskId && field.targets.some(target => target.task.id === lastTaskId);
  }

  let list = $state<HTMLElement>();
  let restoredFocus = false;
  $effect(() => {
    if (restoredFocus || !lastTaskId || !list) return;
    restoredFocus = true;
    list.querySelector<HTMLElement>('[data-contains-last]')?.focus();
  });
</script>

{#snippet writingSystemProgress({task, ws}: Target)}
  {@const p = stats.progress[task.id]}
  <span class="flex items-center gap-1 text-sm {ws ? wsColor(ws) : 'text-muted-foreground'}">
    {#if !p}
      <Skeleton class="size-4 shrink-0 rounded-full" />
    {:else}
      <span aria-hidden="true" class="flex">
        {#if p.remaining === 0}
          <Icon icon="i-mdi-check-circle" class="size-4" />
        {:else}
          <CircularProgress value={p.percentDone} size={16} strokeWidth={2.5} />
        {/if}
      </span>
    {/if}
    {#if ws?.isAudio}<Icon icon="i-mdi-microphone" class="size-4" /><span class="sr-only">{$t`Audio`}</span>{/if}
    {#if ws}
      <span>{ws.abbreviation || ws.name}</span>
      {#if p}<span class="sr-only">, {remainingText(p.remaining)}</span>{/if}
    {:else if p}
      <!-- Nothing to name a language-free task by, so say how much of it is left. -->
      <span class="tabular-nums">{remainingText(p.remaining)}</span>
    {/if}
  </span>
{/snippet}

{#snippet fieldRow(field: Field, props?: Record<string, unknown>)}
  <ListItem {...props} role="listitem" data-contains-last={containsLast(field) || undefined} aria-current={containsLast(field) || undefined}>
    <span class="font-medium">{field.label}</span>
    <!-- Second line so every writing system stays visible, even on a phone. -->
    <span class="mt-1.5 flex flex-wrap items-center gap-x-3 gap-y-1.5">
      {#each field.targets as target (target.task.id)}
        {@render writingSystemProgress(target)}
      {/each}
    </span>
    {#snippet actions()}
      <Icon icon={field.targets.length === 1 ? 'i-mdi-chevron-right' : 'i-mdi-chevron-down'} class="text-muted-foreground shrink-0" />
    {/snippet}
  </ListItem>
{/snippet}

{#if stats.totalEntries === 0}
  <div class="flex flex-col items-start gap-3 px-4">
    <p class="text-muted-foreground">{pt($t`Add some entries first.`, $t`Add some words first.`, viewService.currentView)}</p>
    <Button variant="outline" icon="i-mdi-book-alphabet" onclick={() => navigate(`${$base.uri}/browse`)}>{$t`Browse`}</Button>
  </div>
{:else if fields.length === 0}
  <p class="text-muted-foreground px-4">{$t`No tasks right now.`}</p>
{:else}
  <div class="flex max-w-2xl flex-col gap-2" role="list" bind:this={list}>
    {#each fields as field (field.key)}
      {#if field.targets.length === 1}
        {@render fieldRow(field, {onclick: () => onSelect(field.targets[0].task.id)})}
      {:else}
        <ResponsiveMenu.Root>
          <ResponsiveMenu.Trigger>
            {#snippet child({props})}
              {@render fieldRow(field, props)}
            {/snippet}
          </ResponsiveMenu.Trigger>
          <ResponsiveMenu.Content>
            <div class="text-muted-foreground px-2 py-1.5 text-sm">{$t`Choose a language`}</div>
            {#each field.targets as {task, ws} (task.id)}
              {@const p = stats.progress[task.id]}
              <ResponsiveMenu.Item onSelect={() => onSelect(task.id)}>
                <span aria-hidden="true" class="flex {wsColor(ws!)}">
                  {#if p?.remaining === 0}<Icon icon="i-mdi-check-circle" class="size-4" />{:else}<CircularProgress value={p?.percentDone ?? 0} size={16} strokeWidth={2.5} />{/if}
                </span>
                {#if ws!.isAudio}<Icon icon="i-mdi-microphone" class="size-4 {wsColor(ws!)}" />{/if}
                <span class="truncate">{ws!.name}</span>
                {#if p}<span class="text-muted-foreground ms-auto ps-2 text-xs tabular-nums">{remainingText(p.remaining)}</span>{/if}
              </ResponsiveMenu.Item>
            {/each}
          </ResponsiveMenu.Content>
        </ResponsiveMenu.Root>
      {/if}
    {/each}
  </div>
{/if}
