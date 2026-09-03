<script lang="ts">
  import ListItem from '$lib/components/ListItem.svelte';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {useWritingSystemService} from '$project/data';
  import {useFeatures} from '$lib/services/feature-service';
  import {type IWritingSystem, WritingSystemType} from '$lib/dotnet-types';
  import {useTasksService, type Task} from './tasks-service';

  let {onSelect}: {onSelect: (taskId: string) => void} = $props();

  const tasksService = useTasksService();
  const writingSystemService = useWritingSystemService();
  const features = useFeatures();

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
    return groups;
  });

  function wsColor(ws: IWritingSystem): string {
    return writingSystemService.wsColor(ws.wsId, ws.type === WritingSystemType.Vernacular ? 'vernacular' : 'analysis');
  }
</script>

{#snippet writingSystemLabel(ws: IWritingSystem)}
  {#if ws.isAudio}
    <Icon icon="i-mdi-microphone" class="size-4" />
  {/if}
  <span class={wsColor(ws)}>{ws.abbreviation}</span>
{/snippet}

<div class="flex flex-col gap-2 w-full max-w-3xl mx-auto" role="list">
  {#each fields as {label, targets} (label)}
    {#if targets.length === 1}
      {@const {task, ws} = targets[0]}
      <ListItem role="listitem" onclick={() => onSelect(task.id)}>
        <span class="truncate">{label}</span>
        {#snippet actions()}
          {#if ws}
            <span class="flex items-center gap-1 text-sm" title={ws.name}>{@render writingSystemLabel(ws)}</span>
          {/if}
          <Icon icon="i-mdi-chevron-right" class="text-muted-foreground" />
        {/snippet}
      </ListItem>
    {:else}
      <div role="listitem" class="bg-muted rounded shadow-sm px-4 py-2 flex items-center gap-4 flex-wrap">
        <span class="grow truncate">{label}</span>
        <div class="flex flex-wrap gap-2">
          {#each targets as {task, ws} (task.id)}
            <Button variant="outline" size="sm" onclick={() => onSelect(task.id)} title={ws?.name} aria-label={task.subject}>
              {@render writingSystemLabel(ws!)}
            </Button>
          {/each}
        </div>
      </div>
    {/if}
  {/each}
</div>
