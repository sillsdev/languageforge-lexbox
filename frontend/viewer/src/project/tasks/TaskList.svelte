<script lang="ts">
  import ListItem from '$lib/components/ListItem.svelte';
  import {Icon} from '$lib/components/ui/icon';
  import * as Card from '$lib/components/ui/card';
  import {useWritingSystemService} from '$project/data';
  import type {IWritingSystem} from '$lib/dotnet-types';
  import type {Snippet} from 'svelte';
  import {t} from 'svelte-i18n-lingui';
  import {useTasksService, type Task} from './tasks-service';

  let {onSelect}: {onSelect: (taskId: string) => void} = $props();

  const tasksService = useTasksService();
  const writingSystemService = useWritingSystemService();
  const tasks = $derived(tasksService.listTasks());

  function wsGroups(writingSystems: IWritingSystem[], type: 'vernacular' | 'analysis') {
    return writingSystems
      .map(ws => ({ws, tasks: tasks.filter(task => task.subjectWritingSystemId === ws.wsId && task.subjectWritingSystemType === ws.type)}))
      .filter(group => group.tasks.length > 0)
      .map(group => ({...group, color: writingSystemService.wsColor(group.ws.wsId, type)}));
  }
  const vernacularGroups = $derived(wsGroups(writingSystemService.vernacular, 'vernacular'));
  const analysisGroups = $derived(wsGroups(writingSystemService.analysis, 'analysis'));
  const wsIndependentTasks = $derived(tasks.filter(task => !task.subjectWritingSystemId));
</script>

{#snippet taskCard(tasks: Task[], title: Snippet)}
  <Card.Root>
    <Card.Header>
      <Card.Title>{@render title()}</Card.Title>
    </Card.Header>
    <Card.Content class="flex flex-col gap-2">
      {#each tasks as task (task.id)}
        <ListItem class="py-2" onclick={() => onSelect(task.id)}>
          <span class="truncate">{task.subject}</span>
          {#snippet actions()}
            <Icon icon="i-mdi-chevron-right" class="text-muted-foreground" />
          {/snippet}
        </ListItem>
      {/each}
    </Card.Content>
  </Card.Root>
{/snippet}

{#snippet wsSection(heading: string, groups: ReturnType<typeof wsGroups>)}
  {#if groups.length > 0}
    <section class="space-y-4">
      <h2 class="text-lg font-semibold">{heading}</h2>
      <div class="grid gap-4 lg:grid-cols-2">
        {#each groups as {ws, tasks, color} (ws.wsId)}
          {#snippet wsTitle()}
            <span class={color}>{ws.abbreviation}</span>
            <span class="text-sm font-normal text-muted-foreground ml-2">{ws.name}</span>
          {/snippet}
          {@render taskCard(tasks, wsTitle)}
        {/each}
      </div>
    </section>
  {/if}
{/snippet}

<div class="flex flex-col gap-6 pb-4">
  {@render wsSection($t`Vernacular writing systems`, vernacularGroups)}
  {@render wsSection($t`Analysis writing systems`, analysisGroups)}
  <div class="grid gap-4 lg:grid-cols-2">
    {#snippet anyWsTitle()}{$t`Any writing system`}{/snippet}
    {@render taskCard(wsIndependentTasks, anyWsTitle)}
  </div>
</div>
