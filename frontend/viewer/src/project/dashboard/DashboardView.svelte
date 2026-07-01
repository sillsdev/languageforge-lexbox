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

<div class="flex flex-col h-full p-4 gap-4">
  <div class="flex flex-row items-center">
    <SidebarTrigger icon="i-mdi-menu" class="aspect-square p-0 mr-2" />
    <h1 class="text-xl font-semibold">{$t`Dashboard`}</h1>
  </div>

  <ViewErrorBoundary class="flex-1 min-h-0 overflow-auto" title={$t`Dashboard failed to load`}>
    {#if dashboard}
      {@const totalFormatted = formatNumber(dashboard.totalEntries)}
      {@const goalFormatted = formatNumber(dashboard.goalTarget)}
      {@const percentComplete = formatPercent(dashboard.goalPercent)}
      {@const remainingFormatted = formatNumber(dashboard.goalRemaining)}
      <div class="flex flex-col gap-6 pb-4">
        <Card.Root>
          <Card.Header>
            <Card.Title>{$t`10,000 entry goal`}</Card.Title>
            <Card.Description>
              {$t`${totalFormatted} of ${goalFormatted} entries`}
            </Card.Description>
          </Card.Header>
          <Card.Content class="space-y-3">
            <Progress value={dashboard.goalPercent} max={100} />
            <div class="flex flex-wrap gap-x-6 gap-y-1 text-sm text-muted-foreground">
              <span>{$t`${percentComplete} complete`}</span>
              <span>{$t`${remainingFormatted} remaining`}</span>
            </div>
          </Card.Content>
        </Card.Root>

        <div class="grid gap-4 sm:grid-cols-3">
          <Card.Root>
            <Card.Header>
              <Card.Title>{$t`Total entries`}</Card.Title>
            </Card.Header>
            <Card.Content>
              <p class="text-3xl font-semibold tabular-nums">{formatNumber(dashboard.totalEntries)}</p>
            </Card.Content>
          </Card.Root>
          <Card.Root>
            <Card.Header>
              <Card.Title>{$t`Entries with senses`}</Card.Title>
            </Card.Header>
            <Card.Content>
              <p class="text-3xl font-semibold tabular-nums">{formatNumber(dashboard.entriesWithSenses)}</p>
              <p class="text-sm text-muted-foreground mt-1">
                {formatCompletion(
                  {filled: dashboard.entriesWithSenses, percent: dashboard.totalEntries === 0 ? 0 : Math.round((dashboard.entriesWithSenses / dashboard.totalEntries) * 100)},
                  dashboard.totalEntries,
                )}
              </p>
            </Card.Content>
          </Card.Root>
          <Card.Root>
            <Card.Header>
              <Card.Title>{$t`Entries with examples`}</Card.Title>
            </Card.Header>
            <Card.Content>
              <p class="text-3xl font-semibold tabular-nums">{formatNumber(dashboard.entriesWithExamples)}</p>
              <p class="text-sm text-muted-foreground mt-1">
                {formatCompletion(
                  {filled: dashboard.entriesWithExamples, percent: dashboard.totalEntries === 0 ? 0 : Math.round((dashboard.entriesWithExamples / dashboard.totalEntries) * 100)},
                  dashboard.totalEntries,
                )}
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
      </div>
    {:else}
      <p class="text-muted-foreground p-4">{$t`Loading dashboard...`}</p>
    {/if}
  </ViewErrorBoundary>
</div>
