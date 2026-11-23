<script lang="ts">
  import ChoiceRow from '$lib/components/ChoiceRow.svelte';
  import * as Card from '$lib/components/ui/card';
  import {Icon} from '$lib/components/ui/icon';
  import {Button} from '$lib/components/ui/button';
  import {Skeleton} from '$lib/components/ui/skeleton';
  import {useWritingSystemService} from '$project/data';
  import {useFeatures} from '$lib/services/feature-service';
  import {type IWritingSystem, WritingSystemType} from '$lib/dotnet-types';
  import {plural, t} from 'svelte-i18n-lingui';
  import {navigate, useRouter} from 'svelte-routing';
  import {taskLabel, useTasksService, type Task} from './tasks-service';
  import {getEntityConfig} from '$lib/views/entity-config';
  import {pt, tvt, type ViewText} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {QueryParamState} from '$lib/utils/url.svelte';
  import {useProjectStorage} from '$lib/storage';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {useTasksStats} from './tasks-stats.svelte';
  import {tick} from 'svelte';
  import {watch} from 'runed';

  let {onSelect, lastTaskId}: {onSelect: (taskId: string) => void, lastTaskId?: string} = $props();

  const tasksService = useTasksService();
  const writingSystemService = useWritingSystemService();
  const features = useFeatures();
  const viewService = useViewService();
  const projectStorage = useProjectStorage();
  const {base} = useRouter();

  type Field = {key: string, entity: 'entry' | 'sense' | 'example', label: string, description?: string, tasks: Task[]};

  const fieldParam = new QueryParamState({key: 'field', allowBack: true, replaceOnDefaultValue: true});

  function text(value: ViewText): string {
    return pt($tvt(value), viewService.currentView);
  }

  function writingSystemOf(task: Task): IWritingSystem | undefined {
    if (!task.subjectWritingSystemId) return undefined;
    const writingSystems = task.subjectWritingSystemType === WritingSystemType.Vernacular
      ? writingSystemService.vernacular
      : writingSystemService.analysis;
    return writingSystems.find(ws => ws.wsId === task.subjectWritingSystemId);
  }

  const fields = $derived.by(() => {
    const groups: Field[] = [];
    for (const task of tasksService.listTasks()) {
      // The editors hide audio writing systems when the feature is off, so there'd be nothing to fill in.
      if (writingSystemOf(task)?.isAudio && !features.audio) continue;
      const key = `${task.subjectType}:${task.subjectFields.join()}`;
      let field = groups.find(f => f.key === key);
      if (!field) {
        field = {
          key,
          entity: task.subjectType === 'example-sentence' ? 'example' : task.subjectType,
          label: text(taskLabel(task)),
          description: task.description && text(task.description),
          tasks: [],
        };
        groups.push(field);
      }
      field.tasks.push(task);
    }
    return groups;
  });

  // Meanings first: WeSay's common tasks lead, and word-level fields shouldn't take the top of the page.
  const entities = $derived((['sense', 'example', 'entry'] as const)
    .map(entity => ({entity, label: text(getEntityConfig(entity).$label), fields: fields.filter(f => f.entity === entity)}))
    .filter(group => group.fields.length > 0));

  const stats = $derived(useTasksStats().current);
  function remaining(task: Task): number | undefined {
    return stats.progress[task.id]?.remaining;
  }
  function isDone(task: Task): boolean {
    return remaining(task) === 0;
  }

  // Desktop shows the first field's languages until the user picks one; phones drill in instead.
  const pickedField = $derived(fields.find(f => f.key === fieldParam.current && f.tasks.length > 1));
  const selectedField = $derived(pickedField ?? (IsMobile.value ? undefined : entities.flatMap(e => e.fields).find(f => f.tasks.length > 1)));
  const drilledIn = $derived(IsMobile.value && !!selectedField);
  const languageGroups = $derived.by(() => {
    const tasks = selectedField?.tasks ?? [];
    const written = tasks.filter(task => !writingSystemOf(task)?.isAudio);
    const audio = tasks.filter(task => writingSystemOf(task)?.isAudio);
    if (!written.length || !audio.length) return [{label: '', tasks}];
    return [{label: $t`Written`, tasks: written}, {label: $t`Audio`, tasks: audio}];
  });

  const allTasks = $derived(fields.flatMap(f => f.tasks));
  const recentIds = $derived(projectStorage.recentTasks.current.split(',').filter(Boolean));
  const recentTasks = $derived(recentIds
    .map(id => allTasks.find(task => task.id === id))
    .filter((task): task is Task => !!task && !isDone(task))
    .slice(0, 3));

  let helpOpen = $state(false);
  const showHelp = $derived(helpOpen || (!projectStorage.tasksHelpDismissed.loading && !projectStorage.tasksHelpDismissed.current));
  function dismissHelp() {
    helpOpen = false;
    void projectStorage.tasksHelpDismissed.set('true');
  }

  function openTask(task: Task) {
    void projectStorage.recentTasks.set([task.id, ...recentIds.filter(id => id !== task.id)].slice(0, 5).join());
    // Once tasks have been used the card has done its job, and it would push Recent down.
    if (!projectStorage.tasksHelpDismissed.current) dismissHelp();
    onSelect(task.id);
  }
  function pickField(field: Field) {
    if (field.tasks.length === 1) openTask(field.tasks[0]);
    else fieldParam.current = field.key;
  }

  let container = $state<HTMLElement>();
  function focusFirst(selector: string) {
    [...container?.querySelectorAll<HTMLElement>(selector) ?? []].find(el => el.checkVisibility())?.focus();
  }
  watch(() => lastTaskId, () => {
    if (lastTaskId) void tick().then(() => focusFirst(`[data-task-id="${lastTaskId}"]`));
  });
  watch(() => pickedField?.key, (key, previousKey) => {
    if (!IsMobile.value) return;
    void tick().then(() => focusFirst(key ? '#task-languages-heading' : `[data-field-key="${previousKey}"]`));
  }, {lazy: true});

  function remainingText(count: number): string {
    return pt(
      $plural(count, {one: '# entry to go', other: '# entries to go'}),
      $plural(count, {one: '# word to go', other: '# words to go'}),
      viewService.currentView);
  }
</script>

{#snippet wsName(ws: IWritingSystem)}
  {@const color = writingSystemService.wsColor(ws.wsId, ws.type === WritingSystemType.Vernacular ? 'vernacular' : 'analysis')}
  <span class="inline-flex items-baseline gap-1.5 {color}">
    {ws.name}
    {#if ws.isAudio}
      <Icon icon="i-mdi-microphone" class="size-4 self-center" /><span class="sr-only">{$t`Audio`}</span>
    {:else if ws.abbreviation}
      <span class="text-muted-foreground text-xs">{ws.abbreviation}</span>
    {/if}
  </span>
{/snippet}

{#snippet progress(task: Task)}
  {@const count = remaining(task)}
  {#if count === undefined}
    <Skeleton class="my-0.5 h-4 w-24" />
  {:else}
    {remainingText(count)}
  {/if}
{/snippet}

{#snippet doneMark()}
  <span class="text-muted-foreground flex shrink-0 items-center gap-1 text-sm">
    <Icon icon="i-mdi-check-circle-outline" class="size-4" />{$t`Done`}
  </span>
{/snippet}

{#snippet taskRow(task: Task, withField: boolean)}
  {@const ws = writingSystemOf(task)}
  <li>
    <ChoiceRow data-task-id={task.id} class={isDone(task) ? 'text-muted-foreground' : ''} onclick={() => openTask(task)}>
      <span class="flex flex-wrap items-baseline gap-x-2 font-medium">
        {#if withField}
          {text(taskLabel(task))}
          {#if ws}<span aria-hidden="true" class="text-muted-foreground">·</span>{/if}
        {/if}
        {#if ws}{@render wsName(ws)}{/if}
      </span>
      {#if !isDone(task)}<span class="text-muted-foreground text-sm tabular-nums">{@render progress(task)}</span>{/if}
      {#snippet trailing()}
        {#if isDone(task)}{@render doneMark()}{/if}
      {/snippet}
    </ChoiceRow>
  </li>
{/snippet}

{#if stats.totalEntries === 0}
  <div class="flex flex-col items-start gap-3 px-4">
    <p class="text-muted-foreground">{pt($t`Add some entries first.`, $t`Add some words first.`, viewService.currentView)}</p>
    <Button variant="outline" icon="i-mdi-book-alphabet" onclick={() => navigate(`${$base.uri}/browse`)}>{$t`Browse`}</Button>
  </div>
{:else}
  <div class="flex max-w-[55.5rem] flex-col gap-8" bind:this={container}>
    {#if !drilledIn}
      <div class="flex flex-col gap-4">
        <div class="flex items-center gap-1">
          <p class="text-muted-foreground">{$t`Fill in what's missing, a few at a time.`}</p>
          {#if !showHelp}
            <Button variant="ghost" size="icon-sm" icon="i-mdi-help-circle-outline" aria-label={$t`How tasks work`} onclick={() => helpOpen = true} />
          {/if}
        </div>
        {#if showHelp}
          <Card.Root class="gap-3 py-4">
            <Card.Header>
              <Card.Title>{$t`How tasks work`}</Card.Title>
            </Card.Header>
            <Card.Content class="flex flex-col items-start gap-3">
              <ol class="flex list-decimal flex-col gap-1 ps-5">
                <li>{$t`Pick what's missing, like a Gloss or an Example sentence.`}</li>
                <li>{$t`Pick the language to fill it in.`}</li>
                <li>{pt($t`You'll see the entries that need it, a few at a time. Fill them in or skip them.`, $t`You'll see the words that need it, a few at a time. Fill them in or skip them.`, viewService.currentView)}</li>
              </ol>
              <Button variant="secondary" size="sm" onclick={dismissHelp}>{$t`Got it`}</Button>
            </Card.Content>
          </Card.Root>
        {/if}
      </div>
      {#if recentTasks.length}
        <section class="flex flex-col gap-3">
          <h2 class="text-base font-semibold">{$t`Recent`}</h2>
          <ul class="flex flex-col divide-y overflow-hidden rounded-lg border">
            {#each recentTasks as task (task.id)}
              {@render taskRow(task, true)}
            {/each}
          </ul>
        </section>
      {/if}
    {/if}

    <section class="flex flex-col gap-3">
      {#if !drilledIn}<h2 class="text-base font-semibold">{$t`All tasks`}</h2>{/if}
      <div class="grid items-start gap-6 md:grid-cols-[minmax(0,18rem)_minmax(0,36rem)]">
        {#if !drilledIn}
          <div class="flex flex-col gap-5">
            {#each entities as {entity, label, fields: entityFields} (entity)}
              <div class="flex flex-col gap-2">
                <h3 class="text-muted-foreground text-sm font-medium">{label}</h3>
                <ul class="flex flex-col divide-y overflow-hidden rounded-lg border">
                  {#each entityFields as field (field.key)}
                    {@const single = field.tasks.length === 1 ? field.tasks[0] : undefined}
                    {@const done = field.tasks.every(isDone)}
                    <li>
                      <ChoiceRow
                        data-field-key={field.key}
                        data-task-id={single?.id}
                        selected={field.key === selectedField?.key}
                        class={done ? 'text-muted-foreground' : ''}
                        onclick={() => pickField(field)}>
                        <span class="font-medium">{field.label}</span>
                        <span class="text-muted-foreground text-sm">{field.description}</span>
                        {#if single && !done}
                          <span class="text-muted-foreground text-sm tabular-nums">{@render progress(single)}</span>
                        {/if}
                        {#snippet trailing()}
                          {#if done}{@render doneMark()}{/if}
                        {/snippet}
                      </ChoiceRow>
                    </li>
                  {/each}
                </ul>
              </div>
            {/each}
          </div>
        {/if}

        {#if selectedField}
          <div class="flex flex-col gap-4 md:sticky md:top-0">
            <div class="flex items-start gap-1">
              {#if drilledIn}
                <Button variant="ghost" size="icon" class="shrink-0" icon="i-mdi-arrow-left" aria-label={$t`Back to fields`} onclick={() => fieldParam.current = ''} />
              {/if}
              <div class="flex min-w-0 flex-col">
                <h3 id="task-languages-heading" tabindex="-1" class="text-base font-semibold outline-none">{selectedField.label}</h3>
                <p class="text-muted-foreground text-sm">{selectedField.description}</p>
              </div>
            </div>
            {#each languageGroups as group (group.label)}
              <div class="flex flex-col gap-2">
                {#if group.label}<h4 class="text-muted-foreground text-sm font-medium">{group.label}</h4>{/if}
                <ul class="flex flex-col divide-y overflow-hidden rounded-lg border">
                  {#each group.tasks as task (task.id)}
                    {@render taskRow(task, false)}
                  {/each}
                </ul>
              </div>
            {/each}
          </div>
        {/if}
      </div>
    </section>
  </div>
{/if}
