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
    return groups.filter(group => group.targets.length > 0);
  });

  function wsColor(ws: IWritingSystem): string {
    return writingSystemService.wsColor(ws.wsId, ws.type === WritingSystemType.Vernacular ? 'vernacular' : 'analysis');
  }
</script>

{#snippet writingSystemLabel(ws: IWritingSystem)}
  {#if ws.isAudio}
    <Icon icon="i-mdi-microphone" class="size-4 {wsColor(ws)}" />
  {:else}
    <span class="{wsColor(ws)} size-2 shrink-0 rounded-full bg-current" aria-hidden="true"></span>
  {/if}
  <span class="truncate">{ws.name}</span>
  <span class="text-muted-foreground text-xs shrink-0">{ws.abbreviation}</span>
{/snippet}

<div class="flex flex-col gap-2 w-full max-w-2xl mx-auto" role="list">
  {#each fields as {label, targets} (label)}
    {#if targets.length === 1}
      {@const {task, ws} = targets[0]}
      <ListItem role="listitem" onclick={() => onSelect(task.id)}>
        <span class="truncate">{label}</span>
        {#snippet actions()}
          {#if ws}
            <span class="flex items-center gap-2 text-sm min-w-0" title={ws.name}>{@render writingSystemLabel(ws)}</span>
          {/if}
          <Icon icon="i-mdi-chevron-right" class="text-muted-foreground shrink-0" />
        {/snippet}
      </ListItem>
    {:else}
      <!-- Matches ListItem's shell so labels line up with the rows above and below. -->
      <div role="listitem" class="w-full px-4 py-3 flex flex-col gap-2 bg-muted rounded shadow-sm border-l-5 border-l-transparent">
        <span class="truncate">{label}</span>
        <div class="flex flex-wrap gap-2">
          {#each targets as {task, ws} (task.id)}
            <Button variant="outline" size="sm" class="max-w-full"
                    onclick={() => onSelect(task.id)}
                    title={ws!.name}
                    aria-label={`${label}: ${ws!.name} (${ws!.abbreviation})`}>
              {@render writingSystemLabel(ws!)}
            </Button>
          {/each}
        </div>
      </div>
    {/if}
  {/each}
</div>
