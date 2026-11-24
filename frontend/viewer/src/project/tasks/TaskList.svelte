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

  type TaskOrigin = 'recent' | 'list';
  let {onSelect, lastTask}: {
    onSelect: (taskId: string, origin: TaskOrigin) => void,
    lastTask?: {id: string, origin: TaskOrigin},
  } = $props();

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

  function fieldKey(task: Task): string {
    return `${task.subjectType}:${task.subjectFields.join()}`;
  }

  const fields = $derived.by(() => {
    const groups: Field[] = [];
    for (const task of tasksService.listTasks()) {
      // The editors hide audio writing systems when the feature is off, so there'd be nothing to fill in.
      if (writingSystemOf(task)?.isAudio && !features.audio) continue;
      const key = fieldKey(task);
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

  // With only one language there's nothing to choose, so the field's row starts the task itself.
  function directTask(field: Field): Task | undefined {
    return field.tasks.length === 1 ? field.tasks[0] : undefined;
  }
  const pickedField = $derived(fields.find(f => f.key === fieldParam.current && !directTask(f)));
  const hasLanguageStep = $derived(fields.some(f => !directTask(f)));
  const drilledIn = $derived(IsMobile.value && !!pickedField);
  $effect(() => {
    // A stale or hand-edited ?field= would otherwise linger in the URL with nothing selected.
    if (fields.length && fieldParam.current && !pickedField) fieldParam.set('', {replace: true});
  });
  const languageGroups = $derived.by(() => {
    const tasks = pickedField?.tasks ?? [];
    const written = tasks.filter(task => !writingSystemOf(task)?.isAudio);
    const audio = tasks.filter(task => writingSystemOf(task)?.isAudio);
    if (!written.length || !audio.length) return [{label: '', tasks}];
    return [{label: $t`Written`, tasks: written}, {label: $t`Audio`, tasks: audio}];
  });

  const allTasks = $derived(fields.flatMap(f => f.tasks));
  const recentIds = $derived(projectStorage.recentTasks.current.split(',').filter(Boolean));
  const recentTasks = $derived(recentIds
    .map(id => allTasks.find(task => task.id === id))
    // Waiting for the count keeps finished tasks from flashing up while stats load.
    .filter((task): task is Task => !!task && remaining(task) !== undefined && !isDone(task))
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

  function openTask(task: Task, origin: TaskOrigin) {
    // Once tasks have been used the card has done its job, and it would push Recent down.
    if (!projectStorage.tasksHelpDismissed.current) dismissHelp();
    onSelect(task.id, origin);
  }

  function pickField(field: Field, event: MouseEvent) {
    // Desktop just swaps the panel, so Back should leave the page rather than step through every field viewed.
    fieldParam.set(field.key, {replace: !IsMobile.value});
    // event.detail is 0 for keyboard activation. Otherwise keyboard users tab past every remaining field to reach the languages.
    if (!IsMobile.value && event.detail === 0) void tick().then(() => focusFirst('#task-languages-heading'));
  }

  let container = $state<HTMLElement>();
  function focusFirst(...selectors: string[]) {
    for (const selector of selectors) {
      const el = [...container?.querySelectorAll<HTMLElement>(selector) ?? []].find(el => el.checkVisibility());
      if (el) return el.focus();
    }
  }
  watch(() => lastTask, () => {
    if (!lastTask) return;
    const recent = `[data-recent-task-id="${lastTask.id}"]`;
    const row = `[data-task-id="${lastTask.id}"]`;
    const task = allTasks.find(t => t.id === lastTask.id);
    const fallback = task ? `[data-field-key="${fieldKey(task)}"]` : '#task-list-heading';
    void tick().then(() => focusFirst(...(lastTask.origin === 'recent' ? [recent, row] : [row, recent]), fallback));
  });
  watch(() => pickedField?.key, (key, previousKey) => {
    if (!IsMobile.value) return;
    void tick().then(() => focusFirst(key ? '#task-languages-heading' : `[data-field-key="${previousKey}"]`));
  }, {lazy: true});

  // Level with the selected row so its pointer lands on the panel, but never past the list's end (that would stretch the page).
  let fieldColumn = $state<HTMLElement>();
  let panel = $state<HTMLElement>();
  let listHeight = $state(0);
  let panelHeight = $state(0);
  let panelOffset = $state(0);
  // Heights are read directly, not from the bindings: those lag a frame behind a newly picked field.
  watch([() => pickedField?.key, () => listHeight, () => panelHeight], () => {
    const row = pickedField && fieldColumn?.querySelector(`[data-field-key="${pickedField.key}"]`);
    if (!row || !fieldColumn || !panel) return;
    const rowTop = row.getBoundingClientRect().top - fieldColumn.getBoundingClientRect().top;
    panelOffset = Math.max(0, Math.min(rowTop, fieldColumn.offsetHeight - panel.offsetHeight));
  });

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
    <ChoiceRow starts data-task-id={task.id} aria-describedby="task-starts-hint" class={isDone(task) ? 'text-muted-foreground' : ''} onclick={() => openTask(task, 'list')}>
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
  <div class="flex max-w-[57.5rem] flex-col gap-8" bind:this={container}>
    <span id="task-starts-hint" hidden>{$t`Starts the task`}</span>
    {#if !drilledIn}
      <div class="flex flex-col gap-4">
        <p class="text-muted-foreground">
          {$t`Fill in what's missing, a few at a time.`}
          <!-- Plain primary is too dark to read as text on the dark background. -->
          <button id="tasks-help-link" type="button" aria-expanded={showHelp} aria-controls="tasks-help"
            onclick={() => showHelp ? closeHelp() : helpOpen = true}
            class="text-primary inline-flex cursor-pointer items-center gap-1 font-medium whitespace-nowrap hover:underline dark:text-[color-mix(in_oklab,var(--primary)_70%,white)]">
            {$t`How tasks work`}
            <Icon icon="i-mdi-chevron-down" class="size-4 transition-transform {showHelp ? 'rotate-180' : ''}" />
          </button>
        </p>
        {#if showHelp}
          <Card.Root id="tasks-help" class="py-4">
            <Card.Content class="flex flex-col items-start gap-3">
              <ol class="flex list-decimal flex-col gap-1 ps-5">
                <li>{$t`Pick what's missing, like a Gloss or an Example sentence.`}</li>
                <li>{$t`If there's more than one language, pick one.`}</li>
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
          <ul class="grid gap-x-6 gap-y-3 md:grid-cols-[repeat(2,minmax(0,28rem))]">
            {#each recentTasks as task (task.id)}
              {@const ws = writingSystemOf(task)}
              <li>
                <button type="button" data-recent-task-id={task.id} aria-describedby="task-starts-hint" onclick={() => openTask(task, 'recent')}
                  class="bg-card hover:bg-accent focus-visible:ring-ring/50 flex size-full items-center gap-3 rounded-lg border px-4 py-3 text-start focus-visible:ring-[3px] focus-visible:outline-none">
                  <span class="flex min-w-0 grow flex-col">
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
      {#if !drilledIn}<h2 id="task-list-heading" tabindex="-1" class="text-base font-semibold outline-none">{$t`All tasks`}</h2>{/if}
      <div class="grid items-start gap-6 md:grid-cols-[repeat(2,minmax(0,28rem))]">
        {#if !drilledIn}
          <div class="flex flex-col gap-5" bind:this={fieldColumn} bind:offsetHeight={listHeight}>
            {#each entities as {entity, label, fields: entityFields} (entity)}
              <div class="flex flex-col gap-2">
                <h3 class="text-muted-foreground text-sm font-medium">{label}</h3>
                <!-- Rounds the end rows instead of overflow-hidden, which would clip the selected row's pointer. -->
                <ul class="flex flex-col divide-y rounded-lg border [&>li:first-child>button]:rounded-t-lg [&>li:last-child>button]:rounded-b-lg">
                  {#each entityFields as field (field.key)}
                    {@const done = field.tasks.every(isDone)}
                    {@const direct = directTask(field)}
                    {@const ws = direct && writingSystemOf(direct)}
                    {@const selected = field.key === pickedField?.key}
                    <li class="relative">
                      <ChoiceRow
                        data-field-key={field.key}
                        data-task-id={direct?.id}
                        aria-describedby={direct ? 'task-starts-hint' : undefined}
                        {selected}
                        starts={!!direct}
                        class={done ? 'text-muted-foreground' : ''}
                        onclick={(event) => direct ? openTask(direct, 'list') : pickField(field, event)}>
                        <span class="flex flex-wrap items-baseline gap-x-2">
                          <span class="font-medium">{field.label}</span>
                          {#if ws}<span class="text-sm">{@render wsName(ws)}</span>{/if}
                        </span>
                        <!-- Muted grey is too light on the selected tint. -->
                        <span class="text-sm {selected ? 'text-foreground/75' : 'text-muted-foreground'}">{field.description}</span>
                        {#if direct && !done}
                          <span class="text-muted-foreground text-sm tabular-nums">{@render progress(direct)}</span>
                        {/if}
                        {#snippet trailing()}
                          {#if done}{@render doneMark()}{/if}
                        {/snippet}
                      </ChoiceRow>
                      {#if selected && !IsMobile.value}
                        <!-- Points at the language panel. Solid fill matching the row's translucent tint over the page. -->
                        <span aria-hidden="true"
                          class="absolute -end-2.5 top-1/2 size-5 -translate-y-1/2 rotate-45 border-t border-r bg-[color-mix(in_oklab,var(--primary)_15%,var(--background))] dark:bg-[color-mix(in_oklab,var(--primary)_25%,var(--background))] rtl:border-t-0 rtl:border-r-0 rtl:border-b rtl:border-l"></span>
                      {/if}
                    </li>
                  {/each}
                </ul>
              </div>
            {/each}
          </div>
        {/if}

        {#if pickedField}
          <!-- A spacer rather than a margin: sticky can slide the panel up over a sibling, but not over its own margin. -->
          <div class="md:self-stretch">
            <div aria-hidden="true" class="max-md:hidden" style:height="{panelOffset}px"></div>
            <div bind:this={panel} bind:offsetHeight={panelHeight}
              class="flex flex-col gap-4 md:sticky md:top-0 md:bottom-0 md:max-h-[calc(100dvh-6rem)] md:overflow-y-auto md:rounded-lg md:border md:border-primary/25 md:bg-primary/5 md:p-4 md:dark:bg-primary/10">
              <div class="flex items-start gap-1">
                {#if drilledIn}
                  <Button variant="ghost" size="icon" class="shrink-0" icon="i-mdi-arrow-left" iconProps={{class: 'rtl:rotate-180'}} aria-label={$t`Back to all tasks`} onclick={() => fieldParam.current = ''} />
                {/if}
                <div class="flex min-w-0 flex-wrap items-baseline gap-x-2">
                  <h3 id="task-languages-heading" tabindex="-1" class="text-base font-semibold outline-none">{pickedField.label}</h3>
                  <!-- On desktop the selected row beside the panel already shows this. -->
                  {#if drilledIn}<p class="text-muted-foreground text-sm">{pickedField.description}</p>{/if}
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
          </div>
        {:else if hasLanguageStep && !IsMobile.value}
          <!-- mt-7 = the first group heading plus its gap, so it starts level with the first row. -->
          <p class="text-muted-foreground mt-7 rounded-lg border border-dashed p-4 text-sm">
            {$t`Pick what's missing to get started.`}
          </p>
        {/if}
      </div>
    </section>
  </div>
{/if}
