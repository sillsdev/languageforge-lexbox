<script lang="ts">
  import {useHistoryService} from '$lib/services/history-service';
  import {plural, t} from 'svelte-i18n-lingui';
  import {Debounced, resource} from 'runed';
  import ListItem from '$lib/components/ListItem.svelte';
  import {VList} from 'virtua/svelte';
  import {FormatRelativeDate} from '$lib/components/ui/format';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import ListDetailLayout from '$lib/layout/ListDetailLayout.svelte';
  import ViewErrorBoundary from '$lib/layout/ViewErrorBoundary.svelte';
  import {QueryParamStateBool} from '$lib/utils/url.svelte';
  import {ActivityParam} from '$lib/utils/search-params';
  import ActivityItem from './ActivityItem.svelte';
  import ActivityFilter from './ActivityFilter.svelte';
  import Loading from '$lib/components/Loading.svelte';
  import {Icon} from '$lib/components/ui/icon';
  import {AppNotification} from '$lib/notifications/notifications';
  import type {IProjectActivity} from '$lib/dotnet-types';
  import ActivityViewPicker from './ActivityViewPicker.svelte';
  import ChangeSummary from './ChangeSummary.svelte';
  import {summarizeActivity, groupByRootEntry, factCategory, commitBadge, type CommitBadge} from './change-summary';
  import type {IconClass} from '$lib/icon-class';
  import type {ChangeFact, ChangeFactWithSubject} from './change-summary';
  import {FACT_GLYPH} from './fact-glyph';
  import {
    createDefaultActivityFilters,
    emptyActivityLoad,
    hasActiveServerSideFilters,
    MIN_VISIBLE_FILTERED,
    serverQueryKey,
    toServerQuery,
    type ActivityFilters,
    type ActivityLoad,
  } from './utils';
  import AuthorLabel from './AuthorLabel.svelte';

  const historyService = useHistoryService();

  function glyphFor(fact: ChangeFact) {
    return FACT_GLYPH[factCategory(fact)];
  }

  // Row-level badge in the top-right corner: the icon SHAPE classifies every row's change kind;
  // COLOUR is reserved for structural creates/deletes (entries, senses, vocab objects, imports) so
  // routine edits recede and the events worth noticing pop.
  const BADGE_ICON: Record<CommitBadge['category'], IconClass> = {
    added: 'i-mdi-plus',
    removed: 'i-mdi-minus',
    changed: 'i-mdi-pencil-outline',
    reordered: 'i-mdi-swap-vertical',
  };
  function badgeColor(badge: CommitBadge): string {
    if (!badge.structural) return 'text-muted-foreground';
    return badge.category === 'added' ? 'text-emerald-600 dark:text-emerald-400'
      : badge.category === 'removed' ? 'text-destructive'
      : 'text-muted-foreground';
  }
  function badgeLabel(category: CommitBadge['category']): string {
    const labels: Record<CommitBadge['category'], string> = {
      added: $t`Added`,
      removed: $t`Removed`,
      changed: $t`Edited`,
      reordered: $t`Reordered`,
    };
    return labels[category];
  }

  const THRESHOLD = 20;
  const BATCH_SIZE = THRESHOLD * 2;

  let filters = $state<ActivityFilters>(createDefaultActivityFilters());
  let pageCount = $state(1);

  const queryKey = $derived.by(() => serverQueryKey(filters));
  const serverQuery = $derived.by(() => toServerQuery(filters));

  const activity = resource(
    [() => queryKey, () => pageCount, () => serverQuery, () => historyService.loaded],
    async ([key, pages, query, loaded], _previous, current): Promise<ActivityLoad> => {
      if (!loaded) return emptyActivityLoad;

      if (pages > 1 && current.data?.queryKey !== key) {
        return current.data ?? emptyActivityLoad;
      }

      const skip = (pages - 1) * BATCH_SIZE;
      const data = await historyService.activity(skip, BATCH_SIZE, query);
      if (key !== queryKey || pages !== pageCount) {
        return current.data ?? emptyActivityLoad;
      }
      if (!Array.isArray(data)) {
        console.error('Invalid history data', data);
        return {items: [], hasMorePages: false, queryKey: key};
      }
      const prev =
        pages > 1 && current.data?.queryKey === key
          ? current.data.items
          : [];
      return {
        items: [...prev, ...data],
        hasMorePages: data.length >= BATCH_SIZE,
        queryKey: key,
      };
    },
    {initialValue: emptyActivityLoad},
  );

  const loading = new Debounced(() => activity.loading, 0);
  const loadedQueryKey = $derived(activity.current?.queryKey ?? '');
  const awaitingFreshData = $derived(loadedQueryKey !== queryKey);
  const hasMorePages = $derived(
    awaitingFreshData ? true : (activity.current?.hasMorePages ?? true),
  );

  $effect(() => {
    if (activity.error) {
      AppNotification.error($t`Failed to load activity`, activity.error.message);
    }
  });

  const visibleActivity = $derived.by(() => {
    if (awaitingFreshData) {
      return null;
    }
    return activity.current?.items ?? [];
  });

  let selectedRow = $state<IProjectActivity>();
  let vlist = $state<VList<IProjectActivity>>();

  // DESKTOP: the detail is a sibling split pane, so the newest commit auto-selects (the page opens
  // answering "what changed recently?"). MOBILE: the detail is hierarchical (full-screen on tap, back
  // returns to the list), so nothing auto-selects — detailOpen drives navigation, like Browse's entryOpen.
  const detailOpen = new QueryParamStateBool(
    {key: ActivityParam.DetailOpen, allowBack: IsMobile.value, replaceOnDefaultValue: IsMobile.value},
    false,
  );
  const defaultLayout = [35, 65] as const;

  function selectRow(row: IProjectActivity) {
    selectedRow = row;
    detailOpen.current = true;
  }

  $effect(() => {
    const visible = visibleActivity;
    if (!visible?.length) {
      selectedRow = undefined;
      return;
    }
    if (selectedRow && !visible.some(a => a.commitId === selectedRow?.commitId)) {
      selectedRow = undefined;
    }
    if (!selectedRow && !IsMobile.value) {
      selectedRow = visible[0];
    }
  });

  $effect(() => {
    if (!hasActiveServerSideFilters(filters) || activity.loading || visibleActivity === null || !hasMorePages) return;
    const filtered = visibleActivity.length;
    const loaded = activity.current?.items.length ?? 0;
    if (filtered < MIN_VISIBLE_FILTERED && loaded >= (pageCount - 1) * BATCH_SIZE && loaded > 0) {
      pageCount += 1;
    }
  });

  let prevQueryKey = '';
  $effect.pre(() => {
    const key = queryKey;
    if (prevQueryKey && prevQueryKey !== key) {
      pageCount = 1;
    }
    prevQueryKey = key;
  });

  function onListScroll() {
    if (!vlist || visibleActivity === null) return;
    const scrollOffset = vlist.getScrollOffset();
    const endIndex = vlist.findItemIndex(scrollOffset + vlist.getViewportSize());
    if (endIndex + THRESHOLD >= visibleActivity.length && hasMorePages && !activity.loading) {
      pageCount += 1;
    }
  }
</script>

<!-- One change line: an optional change-kind glyph + the summary. `groupHeadword` (set when the line sits
     under an entry-group header) lets ChangeSummary render the subject relative to the header — hidden when
     identical, a muted "› gloss" context token for senses/examples — AND reserves a gutter so wrapped lines
     hang-indent under the text — a bulleted list of the group's changes. Ungrouped rows are one-offs,
     so the glyph sits inline; single-fact commits skip it entirely (the item shows one glyph in its
     top-right corner instead — see below). -->
{#snippet factLine(entry: ChangeFactWithSubject, groupHeadword?: string, hideGlyph = false)}
  {@const glyph = hideGlyph ? undefined : glyphFor(entry.fact)}
  {#if groupHeadword !== undefined}
    <div class="flex items-center gap-1.5">
      <span class="w-3.5 shrink-0 flex justify-center">
        {#if glyph}<Icon icon={glyph.icon} class="size-3.5 {glyph.class}" />{/if}
      </span>
      <span class="min-w-0 line-clamp-2"><ChangeSummary fact={entry.fact} subject={entry.subject} target={entry.target} {groupHeadword} /></span>
    </div>
  {:else}
    <div class="flex items-center gap-1 min-w-0">
      {#if glyph}<Icon icon={glyph.icon} class="size-3.5 shrink-0 {glyph.class}" />{/if}
      <span class="min-w-0 line-clamp-2"><ChangeSummary fact={entry.fact} subject={entry.subject} target={entry.target} /></span>
    </div>
  {/if}
{/snippet}

<ViewErrorBoundary class="flex flex-col h-full" title={$t`Activity view failed`}>
<ListDetailLayout
  detailVisible={!!(selectedRow && detailOpen.current)}
  {defaultLayout}
  listMinSize={20}>
{#snippet list()}
  <div class="flex flex-col h-full p-2 md:p-4 md:pr-0">
  <div class="md:mr-3">
    <ActivityFilter bind:filters>
      {#snippet trailing()}
        <ActivityViewPicker />
      {/snippet}
    </ActivityFilter>
  </div>

  <div class="flex-1 mt-1 overflow-hidden relative">
    {#if activity.error && awaitingFreshData}
      <div class="flex h-full items-center justify-center gap-2 text-muted-foreground">
        <Icon icon="i-mdi-alert-circle-outline" />
        <p>{$t`Failed to load activity`}</p>
      </div>
    {:else if awaitingFreshData}
      <div class="flex h-full items-center justify-center">
        <Loading class="size-8 text-muted-foreground" aria-label={$t`Loading activity`} />
      </div>
    {:else if visibleActivity && visibleActivity.length}
      <VList bind:this={vlist} data={visibleActivity}
             class="h-full p-0.5 md:pr-3 after:h-12 after:block"
             onscroll={onListScroll}
             getKey={row => row.commitId} bufferSize={400}>
        {#snippet children(row)}
          {@const summary = summarizeActivity(row.changes, row.changeInfo, row.changeTypes)}
          <!-- The badge replaces the inline glyph on single-fact rows (it IS that fact's glyph,
               relocated). Multi-fact commits get no badge — see commitBadge — and keep per-fact glyphs. -->
          {@const badge = commitBadge(summary)}
          <ListItem
            onclick={() => selectRow(row)}
            selected={selectedRow?.commitId === row.commitId}
            class="mb-2 relative overflow-hidden">
            {#if summary.entries.length === 0}
              <span>{row.changeName}</span>
            {:else}
              <!-- Facts group by the entry tree they touch: the header line is the group's `lead` (the
                   entry's own create/delete — the main event) when the commit contains one, else the bare
                   bold headword; the entry's other changes list beneath along an indent rail. Single-fact
                   and headwordless groups render inline instead (their line names its own subject). Base
                   text muted so verbs recede; subject header and data chips render foreground. -->
              {@const groups = groupByRootEntry(summary.entries)}
              <div class="space-y-1 text-muted-foreground">
                {#each groups as group, gi (gi)}
                  {#if group.lead && group.facts.length > 0}
                    <div>
                      {@render factLine(group.lead)}
                      <div class="ms-1 space-y-0.5 border-s border-muted-foreground/40 ps-2">
                        {#each group.facts as entry, i (i)}
                          {@render factLine(entry, group.headword ?? '')}
                        {/each}
                      </div>
                    </div>
                  {:else if group.headword && group.facts.length > 1}
                    <div>
                      <div class="font-semibold text-foreground">{group.headword}</div>
                      <div class="ms-1 space-y-0.5 border-s border-muted-foreground/40 ps-2">
                        {#each group.facts as entry, i (i)}
                          {@render factLine(entry, group.headword)}
                        {/each}
                      </div>
                    </div>
                  {:else}
                    {#if group.lead}
                      {@render factLine(group.lead, undefined, !!badge)}
                    {/if}
                    {#each group.facts as entry, i (i)}
                      {@render factLine(entry, undefined, !!badge)}
                    {/each}
                  {/if}
                {/each}
                {#if summary.remaining > 0}
                  <div class="ps-5">{$plural(summary.remaining, {one: '(+# more change)', other: '(+# more changes)'})}</div>
                {/if}
              </div>
            {/if}
            {#if badge}
              <span class="absolute top-1 end-1" role="img" title={badgeLabel(badge.category)} aria-label={badgeLabel(badge.category)}>
                <Icon icon={BADGE_ICON[badge.category]} class="size-3.5 {badgeColor(badge)}" />
              </span>
            {/if}
            <div class="mt-2 text-sm text-muted-foreground flex flex-wrap gap-x-2 justify-between items-center">
              <AuthorLabel class="min-w-0" authorId={row.metadata.authorId} authorName={row.metadata.authorName} />
              <span class="flex items-center gap-1">
                {#if !row.metadata.extraMetadata['SyncDate']}
                  <Icon
                    icon="i-mdi-cloud-off-outline"
                    class="size-4 shrink-0 text-muted-foreground"
                    title={$t`These changes have not been uploaded yet. Ensure you're online and logged in to share your changes.`} />
                {/if}
                <FormatRelativeDate date={row.timestamp}
                        actualDateOptions={{ dateStyle: 'medium', timeStyle: 'short' }}/>
              </span>
            </div>
          </ListItem>
        {/snippet}
      </VList>
    {:else if !loading.current}
      <div class="p-4 text-center opacity-75">{$t`No activity matches these filters`}</div>
    {/if}
  </div>
  </div>
{/snippet}
{#snippet detail()}
  {#if selectedRow}
    <div class="h-full p-2 md:p-4">
      <ActivityItem activity={selectedRow} showHistoryButton
        showClose={IsMobile.value} onClose={() => detailOpen.current = false} />
    </div>
  {:else}
    <div class="flex items-center justify-center h-full text-muted-foreground text-center m-2">
      <p>{$t`Select a change to view details`}</p>
    </div>
  {/if}
{/snippet}
</ListDetailLayout>
</ViewErrorBoundary>
