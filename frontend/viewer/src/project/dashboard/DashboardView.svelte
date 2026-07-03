<script lang="ts">
  import {SidebarTrigger} from '$lib/components/ui/sidebar';
  import ViewErrorBoundary from '$lib/layout/ViewErrorBoundary.svelte';
  import {t} from 'svelte-i18n-lingui';
  import {useDashboardStats, type FieldCompletion} from './dashboard-stats';
  import {useWritingSystemService} from '$project/data';
  import {formatNumber} from '$lib/components/ui/format';
  import * as Card from '$lib/components/ui/card';
  import {Progress} from '$lib/components/ui/progress';
  import {CircularProgress} from '$lib/components/ui/circular-progress';
  import {Icon} from '$lib/components/ui/icon';
  import {Button} from '$lib/components/ui/button';
  import {dashboardSuggestionUrl} from '$lib/about/community-links';

  const stats = useDashboardStats();
  const writingSystemService = useWritingSystemService();
  const dashboard = $derived(stats.current);

  function formatPercent(value: number | undefined): string {
    if (value === undefined) return '';
    return formatNumber(value / 100, {style: 'percent', maximumFractionDigits: 0});
  }

  function formatCompletion(field: FieldCompletion | undefined, total: number | undefined): string {
    if (!field || total === undefined) return '';
    return `${formatNumber(field.filled)} / ${formatNumber(total)} (${formatPercent(field.percent)})`;
  }

  function formatCount(field: FieldCompletion | undefined, total: number | undefined): string {
    if (!field || total === undefined) return '';
    return `${formatNumber(field.filled)} / ${formatNumber(total)}`;
  }

  const fieldStatGridClass =
    'grid gap-y-3 gap-x-1.5 text-sm leading-none grid-cols-[1fr_minmax(5.5rem,auto)_14px]';
</script>

{#snippet fieldStatRow(
  label: string,
  field: FieldCompletion,
  total: number,
  wsColorClass: string,
)}
  <div class="grid grid-cols-subgrid col-span-full items-center">
    <span class="min-w-0">{label}</span>
    <span class="tabular-nums text-muted-foreground text-right whitespace-nowrap"
      >{formatCount(field, total)}</span
    >
    <div class="{wsColorClass} flex size-3.5 items-center justify-center justify-self-end self-center">
      <CircularProgress value={field.percent} />
    </div>
  </div>
{/snippet}

<div class="flex flex-col h-full py-4 gap-4">
  <div class="flex flex-row items-center px-4">
    <SidebarTrigger icon="i-mdi-menu" class="aspect-square p-0 mr-2" />
    <h1 class="text-xl font-semibold">{$t`Dashboard`}</h1>
  </div>

  <ViewErrorBoundary class="flex-1 min-h-0 overflow-auto mr-2" title={$t`Dashboard failed to load`}>
    {#if dashboard}
      {@const totalFormatted = formatNumber(dashboard.totalEntries)}
      {@const milestone = dashboard.milestone}
      <div class="flex flex-col gap-6 pb-4 px-4">
        <Card.Root class="border-primary/30 bg-primary/5">
          {#if dashboard.totalEntries === 0}
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
                <Card.Title class="text-4xl font-semibold tabular-nums">{totalFormatted}</Card.Title>
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
                <Card.Title class="text-4xl font-semibold tabular-nums">{totalFormatted}</Card.Title>
                <Card.Description>{$t`Total entries`}</Card.Description>
              </div>
            </Card.Header>
            <Card.Content>
              <p class="text-sm text-muted-foreground">{$t`You've passed every milestone. What an achievement!`}</p>
            </Card.Content>
          {/if}
        </Card.Root>

        <div class="grid gap-4 sm:grid-cols-2">
          <Card.Root>
            <Card.Header>
              <Card.Title>{$t`Entries with senses`}</Card.Title>
            </Card.Header>
            <Card.Content>
              <p class="text-3xl font-semibold tabular-nums">{formatNumber(dashboard.entriesWithSenses.filled)}</p>
              <p class="text-sm text-muted-foreground mt-1">
                {formatCompletion(dashboard.entriesWithSenses, dashboard.totalEntries)}
              </p>
            </Card.Content>
          </Card.Root>
          <Card.Root>
            <Card.Header>
              <Card.Title>{$t`Entries with examples`}</Card.Title>
            </Card.Header>
            <Card.Content>
              <p class="text-3xl font-semibold tabular-nums">{formatNumber(dashboard.entriesWithExamples.filled)}</p>
              <p class="text-sm text-muted-foreground mt-1">
                {formatCompletion(dashboard.entriesWithExamples, dashboard.totalEntries)}
              </p>
            </Card.Content>
          </Card.Root>
        </div>

        <section class="space-y-4">
          <h2 class="text-lg font-semibold">{$t`Vernacular writing systems`}</h2>
          {#if dashboard.vernacular.length === 0}
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
                    {@render fieldStatRow($t`Lexeme form`, ws.lexemeForm, dashboard.totalEntries, wsColorClass)}
                    {@render fieldStatRow($t`Citation form`, ws.citationForm, dashboard.totalEntries, wsColorClass)}
                    {@render fieldStatRow($t`Example sentences`, ws.exampleSentence, dashboard.totalEntries, wsColorClass)}
                  </Card.Content>
                </Card.Root>
              {/each}
            </div>
          {/if}
        </section>

        <section class="space-y-4">
          <h2 class="text-lg font-semibold">{$t`Analysis writing systems`}</h2>
          {#if dashboard.analysis.length === 0}
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
                    {@render fieldStatRow($t`Gloss`, ws.gloss, dashboard.totalEntries, wsColorClass)}
                    {@render fieldStatRow($t`Definition`, ws.definition, dashboard.totalEntries, wsColorClass)}
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
    {:else}
      <p class="text-muted-foreground p-4">{$t`Loading dashboard...`}</p>
    {/if}
  </ViewErrorBoundary>
</div>
