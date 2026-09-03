<script lang="ts">
  import ListItem from '$lib/components/ListItem.svelte';
  import {Icon} from '$lib/components/ui/icon';
  import {useWritingSystemService} from '$project/data';
  import type {IWritingSystem} from '$lib/dotnet-types';
  import type {Snippet} from 'svelte';
  import {t} from 'svelte-i18n-lingui';
  import {useTasksService, type Task} from './tasks-service';

  let {onSelect}: {onSelect: (taskId: string) => void} = $props();

  const tasksService = useTasksService();
  const writingSystemService = useWritingSystemService();
  const tasks = $derived(tasksService.listTasks());
  // A writing system can be both vernacular and analysis; its tasks share one group.
  const wsGroups = $derived(writingSystemService.uniqueWritingSystems()
    .map(ws => ({ws, tasks: tasks.filter(task => task.subjectWritingSystemId === ws.wsId)}))
    .filter(group => group.tasks.length > 0));
  const wsIndependentTasks = $derived(tasks.filter(task => !task.subjectWritingSystemId));

  function wsColor(ws: IWritingSystem): string {
    const isVernacular = writingSystemService.vernacular.some(v => v.wsId === ws.wsId);
    return writingSystemService.wsColor(ws.wsId, isVernacular ? 'vernacular' : 'analysis');
  }
</script>

{#snippet taskGroup(id: string, tasks: Task[], title: Snippet)}
  <section aria-labelledby={id} class="flex flex-col gap-2">
    <h2 {id} class="text-lg font-semibold mt-2">{@render title()}</h2>
    {#each tasks as task (task.id)}
      <ListItem onclick={() => onSelect(task.id)}>
        <span class="truncate">{task.subject}</span>
        {#snippet actions()}
          <Icon icon="i-mdi-chevron-right" class="text-muted-foreground" />
        {/snippet}
      </ListItem>
    {/each}
  </section>
{/snippet}

<div class="flex flex-col gap-4 w-full max-w-2xl mx-auto">
  {#each wsGroups as {ws, tasks} (ws.wsId)}
    {#snippet wsTitle()}
      <span class={wsColor(ws)}>{ws.abbreviation}</span>
      <span class="text-sm font-normal text-muted-foreground ml-2">{ws.name}</span>
    {/snippet}
    {@render taskGroup(`tasks-ws-${ws.wsId}`, tasks, wsTitle)}
  {/each}
  {#snippet anyWsTitle()}{$t`Any writing system`}{/snippet}
  {@render taskGroup('tasks-any-ws', wsIndependentTasks, anyWsTitle)}
</div>
