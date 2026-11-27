<script lang="ts">
  import {SidebarTrigger} from '$lib/components/ui/sidebar';
  import ViewErrorBoundary from '$lib/layout/ViewErrorBoundary.svelte';
  import {t} from 'svelte-i18n-lingui';
  import {useDashboardStats, type FieldCompletion} from './dashboard-stats.svelte';
  import {useWritingSystemService} from '$project/data';
  import {formatNumber} from '$lib/components/ui/format';
  import * as Card from '$lib/components/ui/card';
  import {Progress} from '$lib/components/ui/progress';
  import {CircularProgress} from '$lib/components/ui/circular-progress';
  import {Skeleton} from '$lib/components/ui/skeleton';
  import {Icon} from '$lib/components/ui/icon';
  import {Button} from '$lib/components/ui/button';
  import {dashboardSuggestionUrl} from '$lib/about/community-links';
  import {Link} from 'svelte-routing';
  import {useTasksService} from '$project/tasks/tasks-service';
  import type {FieldId} from '$lib/views/entity-config';
  import {useFeatures} from '$lib/services/feature-service';
  import {onErrorEvent} from '$lib/errors/global-errors';
  import {pt} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';

  const stats = useDashboardStats();
  const failed = $derived(!!stats.error);
  $effect(() => {
    if (stats.error) onErrorEvent(stats.error);
  });
  const writingSystemService = useWritingSystemService();
  const tasksService = useTasksService();
  const features = useFeatures();
  const viewService = useViewService();
  // Matches the Tasks list: the editors hide audio writing systems when the feature is off.
  function canFill(wsId: string): boolean {
    return !!features.write && (features.audio || !writingSystemService.allWritingSystems().find(ws => ws.wsId === wsId)?.isAudio);
  }
  // Not $derived: the resource's factory always resolves to the same DashboardStats instance,
  // so this reference never changes — reactivity comes from that instance's own $state fields.
  const dashboard = stats.current;

  function formatPercent(value: number): string {
    return formatNumber(value / 100, {style: 'percent', maximumFractionDigits: 0});
  }

  function formatCompletion(field: FieldCompletion): string {
    return `${formatCount(field)} (${formatPercent(field.percent)})`;
  }

  function formatCount(field: FieldCompletion): string {
    return `${formatNumber(field.filled)} / ${formatNumber(field.total)}`;
  }

  const fieldStatGridClass =
    'grid gap-y-3 gap-x-1.5 text-sm leading-none grid-cols-[1fr_minmax(5.5rem,auto)_14px_14px]';
</script>

{#snippet fieldStatRow(
  label: string,
  field: FieldCompletion | undefined,
  wsColorClass: string,
  ws: {wsId: string, abbreviation: string},
  taskField: FieldId,
)}
  {@const task = field && field.filled < field.total && canFill(ws.wsId) ? tasksService.findTask(taskField, ws.wsId) : undefined}
  {#if task}
    <Link
      to="./tasks?taskId={encodeURIComponent(task.id)}"
      class="grid grid-cols-subgrid col-span-full items-center -mx-2 -my-1.5 px-2 py-1.5 rounded-md hover:bg-accent focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50"
    >
      {@render fieldStatCells(label, field, wsColorClass, ws)}
      <Icon icon="i-mdi-chevron-right" class="size-3.5 text-muted-foreground rtl:rotate-180" aria-hidden="true" />
    </Link>
  {:else}
    <div class="grid grid-cols-subgrid col-span-full items-center">
      {@render fieldStatCells(label, field, wsColorClass, ws)}
    </div>
  {/if}
{/snippet}

{#snippet fieldStatCells(
  label: string,
  field: FieldCompletion | undefined,
  wsColorClass: string,
  ws: {abbreviation: string},
)}
  <span class="min-w-0">{label}<span class="sr-only"> ({ws.abbreviation})</span></span>
  {#if field}
    <span class="tabular-nums text-muted-foreground text-right whitespace-nowrap"
      >{formatCount(field)}</span
    >
    <div class="{wsColorClass} flex size-3.5 items-center justify-center justify-self-end self-center">
      <CircularProgress value={field.percent} />
    </div>
  {:else if failed}
    {@render noValue('text-right')}
    <span></span>
  {:else}
    <Skeleton class="h-3.5 w-14 justify-self-end" />
    <Skeleton class="size-3.5 rounded-full justify-self-end" />
  {/if}
{/snippet}

{#snippet noValue(className: string)}
  <span class="text-muted-foreground {className}" aria-hidden="true">–</span>
{/snippet}

{#snippet summaryCard(title: string, field: FieldCompletion | undefined, taskField?: FieldId)}
  {@const task = taskField && field && field.filled < field.total && features.write ? tasksService.findTask(taskField) : undefined}
  {#if task}
    <Link to="./tasks?taskId={encodeURIComponent(task.id)}" class="group rounded-xl focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50">
      {@render summaryCardBody(title, field, 'group-hover:bg-accent')}
    </Link>
  {:else}
    {@render summaryCardBody(title, field)}
  {/if}
{/snippet}

{#snippet summaryCardBody(title: string, field: FieldCompletion | undefined, className = '')}
  <Card.Root class="h-full {className}">
    <Card.Header>
      <Card.Title>{title}</Card.Title>
    </Card.Header>
    <Card.Content>
      {#if field}
        <p class="text-3xl font-semibold tabular-nums">{formatNumber(field.filled)}</p>
        <p class="text-sm text-muted-foreground mt-1">
          {formatCompletion(field)}
        </p>
      {:else if failed}
        {@render noValue('text-3xl')}
      {:else}
        <Skeleton class="h-9 w-20" />
        <Skeleton class="h-5 w-32 mt-1" />
      {/if}
    </Card.Content>
  </Card.Root>
{/snippet}

<div class="flex flex-col h-full py-4 gap-4">
  <div class="flex flex-row items-center px-4">
    <SidebarTrigger icon="i-mdi-menu" class="aspect-square p-0 mr-2" />
    <h1 class="text-xl font-semibold">{$t`Dashboard`}</h1>
  </div>

  <ViewErrorBoundary class="flex-1 min-h-0 overflow-auto mr-2" title={$t`Dashboard failed to load`}>
    {@const milestone = dashboard.milestone}
    <div class="flex flex-col gap-6 pb-4 px-4">
      <div class="flex flex-col gap-4">
      <Card.Root class="border-primary/30 bg-primary/5">
        {#if dashboard.totalEntries === undefined}
          <Card.Header>
            <div class="flex items-baseline gap-2">
              <Icon icon="i-mdi-trending-up" class="size-6 self-center text-primary" />
              {#if failed}
                {@render noValue('text-4xl')}
              {:else}
                <Skeleton class="h-10 w-28 self-center" />
              {/if}
              <Card.Description>{$t`Total entries`}</Card.Description>
            </div>
          </Card.Header>
          {#if !failed}
            <Card.Content class="space-y-2">
              <Skeleton class="h-2 w-full rounded-full" />
              <Skeleton class="h-5 w-64 max-w-full" />
            </Card.Content>
          {/if}
        {:else if dashboard.totalEntries === 0}
          <Card.Header>
            <Card.Title class="flex items-center gap-2">
              <Icon icon="i-mdi-sprout-outline" class="size-6 text-primary" />
              {$t`Your dictionary starts here`}
            </Card.Title>
            <Card.Description>{$t`Add your first entry to start building your dictionary.`}</Card.Description>
          </Card.Header>
        {:else if milestone}
          {@const targetFormatted = formatNumber(milestone.target)}
          {@const remainingFormatted = formatNumber(milestone.remaining)}
          <Card.Header>
            <div class="flex items-baseline gap-2">
              <Icon icon="i-mdi-trending-up" class="size-6 self-center text-primary" />
              <Card.Title data-testid="total-entries" class="text-4xl font-semibold tabular-nums">{formatNumber(dashboard.totalEntries)}</Card.Title>
              <Card.Description>{$t`Total entries`}</Card.Description>
            </div>
          </Card.Header>
          <Card.Content class="space-y-2">
            <Progress value={milestone.percent} max={100} />
            <p class="text-sm text-muted-foreground">
              {$t`Next milestone: ${targetFormatted}. ${remainingFormatted} to go.`}
            </p>
          </Card.Content>
        {:else}
          <Card.Header>
            <div class="flex items-baseline gap-2">
              <Icon icon="i-mdi-trophy-outline" class="size-6 self-center text-primary" />
              <Card.Title data-testid="total-entries" class="text-4xl font-semibold tabular-nums">{formatNumber(dashboard.totalEntries)}</Card.Title>
              <Card.Description>{$t`Total entries`}</Card.Description>
            </div>
          </Card.Header>
          <Card.Content>
            <p class="text-sm text-muted-foreground">{$t`You've passed every milestone. What an achievement!`}</p>
          </Card.Content>
        {/if}
      </Card.Root>

      <div class="grid gap-4 sm:grid-cols-2">
        {@render summaryCard($t`Entries with senses`, dashboard.entriesWithSenses)}
        {@render summaryCard($t`Entries with examples`, dashboard.entriesWithExamples)}
        {@render summaryCard(pt($t`Entries with grammatical info.`, $t`Entries with a part of speech`, viewService.currentView), dashboard.entriesWithPartOfSpeech, 'partOfSpeechId')}
        {@render summaryCard($t`Entries with semantic domains`, dashboard.entriesWithSemanticDomains, 'semanticDomains')}
      </div>
      </div>

      <section class="space-y-4">
        <h2 class="text-lg font-semibold">{$t`Vernacular writing systems`}</h2>
        {#if !dashboard.vernacular}
          {#if failed}
            {@render noValue('text-sm')}
          {:else}
            <div class="grid gap-4 lg:grid-cols-2">
              <Skeleton class="h-36" />
            </div>
          {/if}
        {:else if dashboard.vernacular.length === 0}
          <p class="text-sm text-muted-foreground">{$t`No vernacular writing systems configured.`}</p>
        {:else}
          <div class="grid gap-4 lg:grid-cols-2">
            {#each dashboard.vernacular as ws (ws.wsId)}
              {@const wsColorClass = writingSystemService.wsColor(ws.wsId, 'vernacular')}
              <Card.Root>
                <Card.Header>
                  <Card.Title class={wsColorClass}>{ws.abbreviation}</Card.Title>
                </Card.Header>
                <Card.Content class={fieldStatGridClass}>
                  {@render fieldStatRow($t`Lexeme form`, ws.lexemeForm, wsColorClass, ws, 'lexemeForm')}
                  {@render fieldStatRow($t`Citation form`, ws.citationForm, wsColorClass, ws, 'citationForm')}
                  {@render fieldStatRow($t`Example sentences`, ws.exampleSentence, wsColorClass, ws, 'sentence')}
                </Card.Content>
              </Card.Root>
            {/each}
          </div>
        {/if}
      </section>

      <section class="space-y-4">
        <h2 class="text-lg font-semibold">{$t`Analysis writing systems`}</h2>
        {#if !dashboard.analysis}
          {#if failed}
            {@render noValue('text-sm')}
          {:else}
            <div class="grid gap-4 lg:grid-cols-2">
              <Skeleton class="h-36" />
            </div>
          {/if}
        {:else if dashboard.analysis.length === 0}
          <p class="text-sm text-muted-foreground">{$t`No analysis writing systems configured.`}</p>
        {:else}
          <div class="grid gap-4 lg:grid-cols-2">
            {#each dashboard.analysis as ws (ws.wsId)}
              {@const wsColorClass = writingSystemService.wsColor(ws.wsId, 'analysis')}
              <Card.Root>
                <Card.Header>
                  <Card.Title class={wsColorClass}>{ws.abbreviation}</Card.Title>
                </Card.Header>
                <Card.Content class={fieldStatGridClass}>
                  {@render fieldStatRow($t`Gloss`, ws.gloss, wsColorClass, ws, 'gloss')}
                  {@render fieldStatRow($t`Definition`, ws.definition, wsColorClass, ws, 'definition')}
                  {@render fieldStatRow($t`Example translations`, ws.translation, wsColorClass, ws, 'translations')}
                </Card.Content>
              </Card.Root>
            {/each}
          </div>
        {/if}
      </section>

      <Button
        variant="ghost"
        href={dashboardSuggestionUrl}
        target="_blank"
        rel="noopener noreferrer"
        class="gap-4 p-6 h-auto w-full justify-start whitespace-normal text-start border border-dashed border-muted-foreground/40 rounded-xl">
        <Icon icon="i-mdi-message-question-outline" class="size-8 text-primary" />
        <div class="min-w-0">
          <div class="font-semibold">{$t`What would you like to see here?`}</div>
          <div class="text-sm text-muted-foreground">
            {$t`Tell us how you work and what would help most. Your ideas guide what we build.`}
          </div>
        </div>
        <span class="ml-auto shrink-0">
          <Icon icon="i-mdi-open-in-new" class="size-5 text-muted-foreground" />
        </span>
      </Button>
    </div>
  </ViewErrorBoundary>
</div>
