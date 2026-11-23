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
  // A field with no writing system has nothing to choose, so its row starts the task itself.
  function directTask(field: Field): Task | undefined {
    return field.tasks.length === 1 && !field.tasks[0].subjectWritingSystemId ? field.tasks[0] : undefined;
  }
  const pickedField = $derived(fields.find(f => f.key === fieldParam.current && !directTask(f)));
  const selectedField = $derived(pickedField ?? (IsMobile.value ? undefined : entities.flatMap(e => e.fields).find(f => !directTask(f))));
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

  function closeHelp() {
    dismissHelp();
    void tick().then(() => focusFirst('#tasks-help-link'));
  }

  function openTask(task: Task) {
    void projectStorage.recentTasks.set([task.id, ...recentIds.filter(id => id !== task.id)].slice(0, 5).join());
    // Once tasks have been used the card has done its job, and it would push Recent down.
    if (!projectStorage.tasksHelpDismissed.current) dismissHelp();
    onSelect(task.id);
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

{#snippet languageRow(task: Task)}
  {@const ws = writingSystemOf(task)}
  <li>
    <ChoiceRow starts data-task-id={task.id} class={isDone(task) ? 'text-muted-foreground' : ''} onclick={() => openTask(task)}>
      <span class="font-medium">{#if ws}{@render wsName(ws)}{:else}{pt(task.prompt, viewService.currentView)}{/if}</span>
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
        <!-- Same disclosure link style as "What can a plugin do?" on the plugins page. -->
        <p class="text-muted-foreground">
          {$t`Fill in what's missing, a few at a time.`}
          <button id="tasks-help-link" type="button" aria-expanded={showHelp} aria-controls="tasks-help"
            onclick={() => showHelp ? closeHelp() : helpOpen = true}
            class="text-primary inline-flex cursor-pointer items-center gap-1 font-medium whitespace-nowrap hover:underline">
            {$t`How tasks work`}
            <Icon icon="i-mdi-chevron-down" class="size-4 transition-transform {showHelp ? 'rotate-180' : ''}" />
          </button>
        </p>
        {#if showHelp}
          <Card.Root id="tasks-help" class="py-4">
            <Card.Content class="flex flex-col items-start gap-3">
              <ol class="flex list-decimal flex-col gap-1 ps-5">
                <li>{$t`Pick what's missing, like a Gloss or an Example sentence.`}</li>
                <li>{$t`Pick the language to fill it in.`}</li>
                <li>{pt($t`You'll see the entries that need it, a few at a time. Fill them in or skip them.`, $t`You'll see the words that need it, a few at a time. Fill them in or skip them.`, viewService.currentView)}</li>
              </ol>
              <Button variant="secondary" size="sm" onclick={closeHelp}>{$t`Got it`}</Button>
            </Card.Content>
          </Card.Root>
        {/if}
      </div>
      {#if recentTasks.length}
        <section class="flex flex-col gap-3">
          <h2 class="flex items-center gap-1.5 text-base font-semibold">
            <Icon icon="i-mdi-history" class="text-muted-foreground size-4" />{$t`Recent`}
          </h2>
          <ul class="grid grid-cols-[repeat(auto-fill,minmax(14rem,1fr))] gap-3">
            {#each recentTasks as task (task.id)}
              {@const ws = writingSystemOf(task)}
              <li>
                <button type="button" data-task-id={task.id} onclick={() => openTask(task)}
                  class="bg-card hover:bg-accent focus-visible:ring-ring/50 flex size-full items-center gap-3 rounded-lg border px-4 py-3 text-start focus-visible:ring-[3px] focus-visible:outline-none">
                  <span class="flex min-w-0 grow flex-col">
                    <!-- Field and language share a line on phones to keep the stacked tiles short. -->
                    <span class="flex flex-wrap items-baseline gap-x-2 md:flex-col md:items-start">
                      <span class="font-medium">{text(taskLabel(task))}</span>
                      {#if ws}<span class="text-sm">{@render wsName(ws)}</span>{/if}
                    </span>
                    <span class="text-muted-foreground text-sm tabular-nums">{@render progress(task)}</span>
                  </span>
                  <Icon icon="i-mdi-play" class="text-primary size-5 shrink-0" aria-hidden="true" />
                </button>
              </li>
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
                <!-- Rounds the end rows instead of overflow-hidden, which would clip the selected row's pointer. -->
                <ul class="flex flex-col divide-y rounded-lg border [&>li:first-child>button]:rounded-t-lg [&>li:last-child>button]:rounded-b-lg">
                  {#each entityFields as field (field.key)}
                    {@const done = field.tasks.every(isDone)}
                    {@const direct = directTask(field)}
                    <li class="relative">
                      <ChoiceRow
                        data-field-key={field.key}
                        data-task-id={direct?.id}
                        selected={field.key === selectedField?.key}
                        starts={!!direct}
                        class={done ? 'text-muted-foreground' : ''}
                        onclick={() => direct ? openTask(direct) : fieldParam.current = field.key}>
                        <span class="font-medium">{field.label}</span>
                        <span class="text-muted-foreground text-sm">{field.description}</span>
                        {#if direct && !done}
                          <span class="text-muted-foreground text-sm tabular-nums">{@render progress(direct)}</span>
                        {/if}
                        {#snippet trailing()}
                          {#if done}{@render doneMark()}{/if}
                        {/snippet}
                      </ChoiceRow>
                      {#if field.key === selectedField?.key && !IsMobile.value}
                        <!-- Points at the language panel. Solid fill matching the row's translucent tint over the page. -->
                        <span aria-hidden="true"
                          class="absolute -end-2.5 top-1/2 size-5 -translate-y-1/2 rotate-45 border-t border-r bg-[color-mix(in_oklab,var(--primary)_10%,var(--background))] dark:bg-[color-mix(in_oklab,var(--primary)_20%,var(--background))] rtl:border-t-0 rtl:border-r-0 rtl:border-b rtl:border-l"></span>
                      {/if}
                    </li>
                  {/each}
                </ul>
              </div>
            {/each}
          </div>
        {/if}

        {#if selectedField}
          <!-- Tinted like the selected field row, to show the languages belong to it. -->
          <!-- md:mt-7 = the left column's group heading plus its gap, so the panel starts level with the first row. -->
          <div class="flex flex-col gap-4 md:sticky md:top-0 md:mt-7 md:rounded-lg md:border md:border-primary/25 md:bg-primary/5 md:p-4 md:dark:bg-primary/10">
            <div class="flex items-start gap-1">
              {#if drilledIn}
                <Button variant="ghost" size="icon" class="shrink-0" icon="i-mdi-arrow-left" aria-label={$t`Back to fields`} onclick={() => fieldParam.current = ''} />
              {/if}
              <div class="flex min-w-0 flex-wrap items-baseline gap-x-2">
                <h3 id="task-languages-heading" tabindex="-1" class="text-base font-semibold outline-none">{selectedField.label}</h3>
                <p class="text-muted-foreground text-sm">{selectedField.description}</p>
              </div>
            </div>
            {#each languageGroups as group (group.label)}
              <div class="flex flex-col gap-2">
                {#if group.label}<h4 class="text-muted-foreground text-sm font-medium">{group.label}</h4>{/if}
                <ul class="bg-background flex flex-col divide-y overflow-hidden rounded-lg border">
                  {#each group.tasks as task (task.id)}
                    {@render languageRow(task)}
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
