<script lang="ts">
  import ListItem from '$lib/components/ListItem.svelte';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {Icon} from '$lib/components/ui/icon';
  import {useWritingSystemService} from '$project/data';
  import {useFeatures} from '$lib/services/feature-service';
  import {type IWritingSystem, WritingSystemType} from '$lib/dotnet-types';
  import {useTasksService, type Task} from './tasks-service';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {t} from 'svelte-i18n-lingui';

  let {onSelect}: {onSelect: (taskId: string) => void} = $props();

  const tasksService = useTasksService();
  const writingSystemService = useWritingSystemService();
  const features = useFeatures();
  const shownAbbreviations = $derived(IsMobile.value ? 2 : 4);

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
    const groups: {label: string, targets: Target[]}[] = [];
    for (const task of tasksService.listTasks()) {
      const ws = writingSystemOf(task);
      // The editors hide audio writing systems when the feature is off, so those tasks
      // would open with nothing to fill in.
      if (ws?.isAudio && !features.audio) continue;
      let group = groups.find(g => g.label === task.fieldLabel);
      if (!group) {
        group = {label: task.fieldLabel, targets: []};
        groups.push(group);
      }
      group.targets.push({task, ws});
    }
    for (const group of groups) {
      group.targets.sort((a, b) => Number(a.ws?.isAudio ?? false) - Number(b.ws?.isAudio ?? false));
    }
    return groups.filter(group => group.targets.length > 0);
  });

  function wsColor(ws: IWritingSystem): string {
    return writingSystemService.wsColor(ws.wsId, ws.type === WritingSystemType.Vernacular ? 'vernacular' : 'analysis');
  }
</script>

{#snippet writingSystem(ws: IWritingSystem)}
  {#if ws.isAudio}
    <Icon icon="i-mdi-microphone" class="size-4 {wsColor(ws)}" />
    <span class="sr-only">{$t`Audio`}</span>
  {:else}
    <span class="{wsColor(ws)} size-2 shrink-0 rounded-full bg-current" aria-hidden="true"></span>
  {/if}
{/snippet}

<div class="grid gap-2 lg:grid-cols-2" role="list">
  {#each fields as {label, targets} (label)}
    {#if targets.length === 1}
      {@const {task, ws} = targets[0]}
      <ListItem role="listitem" onclick={() => onSelect(task.id)}>
        <span class="truncate">{label}</span>
        {#snippet actions()}
          {#if ws}
            <span class="flex items-center gap-2 text-sm min-w-0" title={ws.name}>
              {@render writingSystem(ws)}
              <span class="truncate">{ws.name}</span>
              <span class="text-muted-foreground text-xs shrink-0" aria-hidden="true">{ws.abbreviation}</span>
            </span>
          {/if}
          <Icon icon="i-mdi-chevron-right" class="text-muted-foreground shrink-0" />
        {/snippet}
      </ListItem>
    {:else}
      {@const hidden = targets.length - shownAbbreviations}
      <ResponsiveMenu.Root>
        <ResponsiveMenu.Trigger>
          {#snippet child({props})}
            <ListItem {...props} role="listitem">
              <span class="truncate">{label}</span>
              {#snippet actions()}
                <span class="flex items-center gap-2 text-sm shrink-0">
                  {#each targets.slice(0, shownAbbreviations) as {task, ws} (task.id)}
                    <span class="flex items-center gap-1" title={ws!.name}>
                      {@render writingSystem(ws!)}
                      <span>{ws!.abbreviation}</span>
                    </span>
                  {/each}
                  {#if hidden > 0}
                    <span class="text-muted-foreground">{$t`+${hidden}`}</span>
                  {/if}
                </span>
                <Icon icon="i-mdi-chevron-down" class="text-muted-foreground shrink-0" />
              {/snippet}
            </ListItem>
          {/snippet}
        </ResponsiveMenu.Trigger>
        <ResponsiveMenu.Content>
          <div class="px-2 py-1.5 text-sm text-muted-foreground">{$t`Choose a language`}</div>
          {#each targets as {task, ws} (task.id)}
            <ResponsiveMenu.Item onSelect={() => onSelect(task.id)}>
              {@render writingSystem(ws!)}
              <span class="truncate">{ws!.name}</span>
              <span class="text-muted-foreground ms-auto ps-2 text-xs">{ws!.abbreviation}</span>
            </ResponsiveMenu.Item>
          {/each}
        </ResponsiveMenu.Content>
      </ResponsiveMenu.Root>
    {/if}
  {/each}
</div>
